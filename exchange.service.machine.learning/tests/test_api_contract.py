"""Characterize the existing Flask and Celery-facing HTTP contract."""
import unittest
from typing import Any
from unittest.mock import Mock, patch

from dependency_stubs import install_import_stubs

install_import_stubs()

from main import create_app


class _Logger:
    def __init__(self):
        self.messages = []

    def info(self, message):
        self.messages.append(message)


class _Task:
    def __init__(self, task_id="task-123", status="PENDING", info=None):
        self.task_id = task_id
        self.status = status
        self.state = status
        self.info = info


class _Celery:
    def __init__(self, result=None):
        self.result = result or _Task()
        self.calls = []

    def AsyncResult(self, **kwargs):
        self.calls.append(kwargs)
        return self.result


class _Configuration:
    def __init__(self, result: _Task | None = None) -> None:
        self.mi_logger = _Logger()
        self.celery = _Celery(result)
        self.mi_cors_allowed_origins: list[str] = []


class ApiContractTests(unittest.TestCase):
    def setUp(self):
        self.configuration = _Configuration()
        application = create_app(self.configuration)
        application.config.update(TESTING=True)
        self.client = application.test_client()

    @patch("app.views.recurrent_neural_network.Configuration.get_files")
    def test_rnn_get_contract_is_available_at_both_routes(self, get_files):
        get_files.return_value = ["btc-eur.csv", "eth-eur.csv"]

        for route in ("/", "/api/v1/rnn/"):
            with self.subTest(route=route):
                response = self.client.get(route)
                self.assertEqual(200, response.status_code)
                self.assertEqual(
                    {
                        "api_version": "1.0.0",
                        "indicator_files": ["btc-eur.csv", "eth-eur.csv"],
                    },
                    response.get_json())

    def test_predict_get_contract_has_no_cross_origin_access_by_default(self):
        response = self.client.get(
            "/api/v1/predict/", headers={"Origin": "https://unconfigured.example"})

        self.assertEqual(200, response.status_code)
        self.assertEqual({"API VERSION": "1.0.0"}, response.get_json())
        self.assertNotIn("Access-Control-Allow-Origin", response.headers)

    def test_predict_get_cors_allows_only_configured_origins(self) -> None:
        configuration: _Configuration = _Configuration()
        configuration.mi_cors_allowed_origins = ["https://allowed.example"]
        application: Any = create_app(configuration)

        allowed_response: Any = application.test_client().get(
            "/api/v1/predict/", headers={"Origin": "https://allowed.example"})
        denied_response: Any = application.test_client().get(
            "/api/v1/predict/", headers={"Origin": "https://unconfigured.example"})

        self.assertEqual(
            "https://allowed.example",
            allowed_response.headers["Access-Control-Allow-Origin"])
        self.assertNotIn("Access-Control-Allow-Origin", denied_response.headers)

    @patch("app.views.predict.Configuration.is_valid_indicator_file",
           return_value=True)
    @patch("app.views.predict.prediction.delay")
    def test_predict_post_dispatches_prediction(
            self, delay: Mock, valid_indicator: Mock) -> None:
        delay.return_value = _Task(task_id="prediction-1", status="PENDING")

        response: Any = self.client.post(
            "/api/v1/predict/", json={"indicator_file": "btc-eur.csv"})

        self.assertEqual(202, response.status_code)
        self.assertEqual(
            {"task_id": "prediction-1", "status": "PENDING"},
            response.get_json())
        delay.assert_called_once_with("btc-eur.csv")
        valid_indicator.assert_called_once_with("btc-eur.csv")

    @patch("app.views.recurrent_neural_network.Configuration.is_valid_indicator_file",
           return_value=True)
    @patch("app.views.recurrent_neural_network.training.delay")
    def test_rnn_post_dispatches_training(
            self, delay: Mock, valid_indicator: Mock) -> None:
        delay.return_value = _Task(task_id="training-1", status="PENDING")

        response: Any = self.client.post(
            "/api/v1/rnn/", json={"indicator_file": "eth-eur.csv"})

        self.assertEqual(202, response.status_code)
        self.assertEqual(
            {"task_id": "training-1", "status": "PENDING"},
            response.get_json())
        delay.assert_called_once_with("eth-eur.csv")
        valid_indicator.assert_called_once_with("eth-eur.csv")

    @patch("app.views.predict.Configuration.is_valid_indicator_file",
           return_value=False)
    @patch("app.views.predict.prediction.delay")
    def test_predict_rejects_invalid_indicator_without_enqueueing(
            self, delay: Mock, valid_indicator: Mock) -> None:
        response: Any = self.client.post(
            "/api/v1/predict/", json={"indicator_file": "../secret.csv"})

        self.assertEqual(400, response.status_code)
        self.assertEqual({"error": "invalid_indicator_file"}, response.get_json())
        delay.assert_not_called()
        valid_indicator.assert_called_once_with("../secret.csv")

    @patch("app.views.recurrent_neural_network.Configuration.is_valid_indicator_file",
           return_value=False)
    @patch("app.views.recurrent_neural_network.training.delay")
    def test_rnn_rejects_invalid_indicator_without_enqueueing(
            self, delay: Mock, valid_indicator: Mock) -> None:
        response: Any = self.client.post(
            "/api/v1/rnn/", json={"indicator_file": "missing.csv"})

        self.assertEqual(400, response.status_code)
        self.assertEqual({"error": "invalid_indicator_file"}, response.get_json())
        delay.assert_not_called()
        valid_indicator.assert_called_once_with("missing.csv")

    def test_pending_task_status_has_empty_status(self):
        response = self.client.post(
            "/api/v1/taskstatus/", json={"task_id": "task-123"})

        self.assertEqual(200, response.status_code)
        self.assertEqual({"state": "PENDING", "status": {}}, response.get_json())
        self.assertEqual("task-123", self.configuration.celery.calls[0]["id"])

    def test_completed_task_status_deserializes_task_metadata(self):
        configuration = _Configuration(
            _Task(status="SUCCESS", info='{"current_progress": 100}'))
        application = create_app(configuration)
        application.config.update(TESTING=True)

        response = application.test_client().post(
            "/api/v1/taskstatus/", json={"task_id": "task-456"})

        self.assertEqual(200, response.status_code)
        self.assertEqual(
            {"state": "SUCCESS", "status": {"current_progress": 100}},
            response.get_json())

    def test_progress_task_status_normalizes_dict_metadata(self) -> None:
        configuration: _Configuration = _Configuration(
            _Task(status="PROGRESS", info={"progress": 70}))
        application: Any = create_app(configuration)
        application.config.update(TESTING=True)

        response: Any = application.test_client().post(
            "/api/v1/taskstatus/", json={"task_id": "task-789"})

        self.assertEqual(200, response.status_code)
        self.assertEqual(
            {
                "state": "PROGRESS",
                "status": {
                    "current_progress": 70,
                    "total_progress": 100,
                    "status": "Task in progress.",
                },
            },
            response.get_json())

    def test_failure_status_is_generic_and_does_not_log_exception_details(
            self) -> None:
        secret: str = "sentinel-secret-value"
        configuration: _Configuration = _Configuration(
            _Task(status="FAILURE", info=RuntimeError(secret)))
        application: Any = create_app(configuration)
        application.config.update(TESTING=True)

        response: Any = application.test_client().post(
            "/api/v1/taskstatus/", json={"task_id": "task-failed"})

        self.assertEqual(200, response.status_code)
        self.assertEqual(
            {
                "state": "FAILURE",
                "status": {
                    "current_progress": 0,
                    "total_progress": 100,
                    "status": "Task failed.",
                },
            },
            response.get_json())
        self.assertNotIn(secret, response.get_data(as_text=True))
        self.assertNotIn(secret, "".join(configuration.mi_logger.messages))


if __name__ == "__main__":
    unittest.main()
