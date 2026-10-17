"""Characterize secret-free configuration and runtime defaults."""
import logging
import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import Mock, patch, sentinel

from dependency_stubs import install_import_stubs

install_import_stubs()

from app.configuration import Configuration


class ConfigurationContractTests(unittest.TestCase):
    @patch.dict(os.environ, {}, clear=True)
    def test_safe_defaults_do_not_embed_connection_credentials(self):
        configuration = Configuration()

        self.assertEqual("0.0.0.0", configuration.mi_host)
        self.assertEqual(5005, configuration.mi_port)
        self.assertEqual([], configuration.mi_cors_allowed_origins)
        self.assertIsNone(configuration.mi_broker_url)
        self.assertIsNone(configuration.mi_result_backend)
        self.assertIsNone(configuration.mi_database_url)
        self.assertIsNotNone(configuration.celery)
        self.assertIsInstance(configuration.mi_logger, logging.Logger)

    @patch.dict(
        os.environ,
        {
            "MI_HOST": "127.0.0.1",
            "MI_PORT": "5105",
            "MI_BROKER_URL": "mongodb://broker.example/celery",
            "MI_RESULT_BACKEND": "mongodb://results.example/celery",
            "MI_DATABASE_URL": "mongodb://data.example/machinelearning",
            "MI_CORS_ALLOWED_ORIGINS": "https://ui.example.test, http://localhost:9000",
        },
        clear=True)
    def test_environment_configures_web_celery_and_database_boundaries(self):
        configuration = Configuration()

        self.assertEqual("127.0.0.1", configuration.mi_host)
        self.assertEqual(5105, configuration.mi_port)
        self.assertEqual(
            ["https://ui.example.test", "http://localhost:9000"],
            configuration.mi_cors_allowed_origins)
        self.assertEqual(
            "mongodb://broker.example/celery",
            configuration.celery.conf.broker_url)
        self.assertEqual(
            "mongodb://results.example/celery",
            configuration.celery.conf.result_backend)
        self.assertEqual(
            "mongodb://data.example/machinelearning",
            configuration.mi_database_url)

    @patch.dict(os.environ, {"MI_CORS_ALLOWED_ORIGINS": "https://*.example.test"}, clear=True)
    def test_wildcard_cors_origin_fails_closed(self) -> None:
        with self.assertRaisesRegex(ValueError, "must not contain wildcard origins"):
            Configuration()

    @patch.dict(
        os.environ,
        {"MI_BROKER_URL": "mongodb://broker.example/celery"},
        clear=True)
    def test_partial_celery_configuration_fails_closed(self):
        with self.assertRaisesRegex(ValueError, "must be configured together"):
            Configuration()

    @patch.dict(os.environ, {}, clear=True)
    def test_database_access_requires_an_explicit_url(self):
        with self.assertRaisesRegex(RuntimeError, "MI_DATABASE_URL is required"):
            Configuration().get_mongo_database()

    @patch.dict(os.environ, {}, clear=True)
    @patch("app.configuration.MongoClient")
    def test_database_client_uses_only_the_explicit_url(self, mongo_client):
        client = Mock()
        client.machinelearning = sentinel.database
        mongo_client.return_value = client
        configuration = Configuration()
        configuration.mi_database_url = "mongodb://data.example/machinelearning"

        database = configuration.get_mongo_database()

        mongo_client.assert_called_once_with(
            "mongodb://data.example/machinelearning")
        self.assertIs(sentinel.database, database)

    def test_indicator_inventory_is_sorted_and_does_not_change_working_directory(
            self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            static_directory: Path = Path(temporary_directory)
            (static_directory / "eth-eur.csv").write_text("data")
            (static_directory / "btc-eur.csv").write_text("data")
            (static_directory / "README.txt").write_text("data")
            working_directory: str = os.getcwd()

            with patch.object(Configuration, "get_static_directory",
                              return_value=static_directory):
                self.assertEqual(
                    ["btc-eur.csv", "eth-eur.csv"],
                    Configuration.get_files())
                self.assertEqual(working_directory, os.getcwd())

    def test_indicator_validation_rejects_path_tricks_and_unknown_names(
            self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            static_directory: Path = Path(temporary_directory)
            (static_directory / "btc-eur.csv").write_text("data")

            with patch.object(Configuration, "get_static_directory",
                              return_value=static_directory):
                self.assertTrue(Configuration.is_valid_indicator_file(
                    "btc-eur.csv"))
                indicator_files: tuple[str, ...] = (
                        "../btc-eur.csv",
                        "..\\btc-eur.csv",
                        "btc-eur.csv/child",
                        "btc-eur.csv.bak",
                        "missing.csv",
                        "btc-eur.csv\x00")
                for indicator_file in indicator_files:
                    with self.subTest(indicator_file=indicator_file):
                        self.assertFalse(Configuration.is_valid_indicator_file(
                            indicator_file))
                        self.assertIsNone(Configuration.get_file(indicator_file))

                normalizer_path: str = Configuration.get_file_normalized_file(
                    "btc-eur.csv")
                model_path: str = Configuration.get_file_model_file("btc-eur.csv")
                self.assertEqual(
                    static_directory / "btc_eur" / "btc-eur_normalizer.json",
                    Path(normalizer_path))
                self.assertEqual(
                    static_directory / "btc_eur" / "btc-eur_model.h5",
                    Path(model_path))

                with self.assertRaises(ValueError):
                    Configuration.get_directory("../btc-eur.csv")


if __name__ == "__main__":
    unittest.main()
