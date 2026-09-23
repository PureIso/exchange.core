"""Characterize secret-free configuration and runtime defaults."""
import logging
import os
import unittest
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
        },
        clear=True)
    def test_environment_configures_web_celery_and_database_boundaries(self):
        configuration = Configuration()

        self.assertEqual("127.0.0.1", configuration.mi_host)
        self.assertEqual(5105, configuration.mi_port)
        self.assertEqual(
            "mongodb://broker.example/celery",
            configuration.celery.conf.broker_url)
        self.assertEqual(
            "mongodb://results.example/celery",
            configuration.celery.conf.result_backend)
        self.assertEqual(
            "mongodb://data.example/machinelearning",
            configuration.mi_database_url)

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


if __name__ == "__main__":
    unittest.main()
