"""Characterize the existing Flask and Celery-facing HTTP contract."""
import unittest
from unittest.mock import patch

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
    def __init__(self, result=None):
        self.mi_logger = _Logger()
        self.celery = _Celery(result)


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

    def test_predict_get_contract_and_cors(self):
        response = self.client.get(
            "/api/v1/predict/", headers={"Origin": "https://example.test"})

        self.assertEqual(200, response.status_code)
        self.assertEqual({"API VERSION": "1.0.0"}, response.get_json())
        self.assertEqual("https://example.test", response.headers["Access-Control-Allow-Origin"])

    @patch("app.views.predict.prediction.delay")
    def test_predict_post_dispatches_prediction(self, delay):
        delay.return_value = _Task(task_id="prediction-1", status="PENDING")

        response = self.client.post(
            "/api/v1/predict/", json={"indicator_file": "btc-eur.csv"})

        self.assertEqual(202, response.status_code)
        self.assertEqual(
            {"task_id": "prediction-1", "status": "PENDING"},
            response.get_json())
        delay.assert_called_once_with("btc-eur.csv")

    @patch("app.views.recurrent_neural_network.training.delay")
    def test_rnn_post_dispatches_training(self, delay):
        delay.return_value = _Task(task_id="training-1", status="PENDING")

        response = self.client.post(
            "/api/v1/rnn/", json={"indicator_file": "eth-eur.csv"})

        self.assertEqual(202, response.status_code)
        self.assertEqual(
            {"task_id": "training-1", "status": "PENDING"},
            response.get_json())
        delay.assert_called_once_with("eth-eur.csv")

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


if __name__ == "__main__":
    unittest.main()
