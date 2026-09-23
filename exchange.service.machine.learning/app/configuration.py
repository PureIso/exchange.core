"""Application Master Configuration"""
import os
import logging
from pathlib import Path
import graypy
from pymongo import MongoClient
from celery import Celery
from dotenv import load_dotenv


class Configuration():
    """Maching Learning configuration class.
    The default configuration is to run on docker
    """
    NORMALISEJSONFILENAME = 'normalizer.json'
    MODELH5FILENAME = 'model.h5'

    celery = None
    current_training_status = {}
    mi_host = "0.0.0.0"
    mi_port = 5005
    mi_graylog_host = "127.0.0.1"
    mi_graylog_port = 12201
    mi_logger = None
    mi_broker_url = None
    mi_result_backend = None
    mi_database_url = None

    def __init__(self):
        service_directory = Path(__file__).resolve().parent.parent
        self.current_training_status = {}

        environment_file = service_directory / '.env'
        if environment_file.is_file():
            load_dotenv(environment_file)

        self.celery = Celery('machinelearning')
        self.mi_host = os.getenv("MI_HOST", self.mi_host)
        self.mi_port = int(os.getenv("MI_PORT", str(self.mi_port)))
        self.mi_logger = logging.getLogger('mi_logger')
        self.mi_logger.addHandler(logging.NullHandler())

        graylog_host = os.getenv("MI_GRAYLOG_HOST")
        graylog_port = os.getenv("MI_GRAYLOG_PORT")
        if graylog_host and graylog_port:
            self.mi_graylog_host = graylog_host
            self.mi_graylog_port = int(graylog_port)
            handler = graypy.GELFUDPHandler(
                self.mi_graylog_host, self.mi_graylog_port)
            self.mi_logger.setLevel(logging.INFO)
            self.mi_logger.addHandler(handler)

        self.mi_broker_url = os.getenv("MI_BROKER_URL")
        self.mi_result_backend = os.getenv("MI_RESULT_BACKEND")
        self.mi_database_url = os.getenv("MI_DATABASE_URL")
        if bool(self.mi_broker_url) != bool(self.mi_result_backend):
            raise ValueError(
                "MI_BROKER_URL and MI_RESULT_BACKEND must be configured together")
        if self.mi_broker_url and self.mi_result_backend:
            self.celery.conf.update(
                broker_url=self.mi_broker_url,
                result_backend=self.mi_result_backend,
                broker_use_ssl=False,
                taskmeta_collection='celery_taskmeta',
                groupmeta_collection='celery_groupmeta',
                max_pool_size=10,
                options=None
            )
        self.mi_logger.info('Configuration loading')
        super(Configuration, self).__init__()

    def get_mongo_database(self):
        """[summary]

        Returns:
            [type]: [description]
        """
        if not self.mi_database_url:
            raise RuntimeError("MI_DATABASE_URL is required for database access")
        mongodb_client = MongoClient(self.mi_database_url)
        return mongodb_client.machinelearning

    @staticmethod
    def get_files():
        """[summary]

        Returns:
            [type]: [description]
        """
        indicator_files = []
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        directory = os.path.join(directory, "static")
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(".csv"):
                indicator_files.append(file_name)
        return indicator_files

    @staticmethod
    def get_file(indicator_file):
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        directory = os.path.join(directory, "static")
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(indicator_file):
                return os.path.join(directory, file_name)
        return None

    @staticmethod
    def get_file_normalized_file(indicator_file):
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        directory = Configuration.get_directory(indicator_file)
        indicator_file = indicator_file.replace(".csv", "_normalizer.json")
        if not os.path.exists(directory):
            os.makedirs(directory)
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(indicator_file):
                return os.path.join(directory, file_name)
        return os.path.join(directory, indicator_file)

    @staticmethod
    def get_file_model_file(indicator_file):
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        indicator_model_file = indicator_file.replace(".csv", "_model.h5")
        directory = Configuration.get_directory(indicator_file)
        if not os.path.exists(directory):
            os.makedirs(directory)
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(indicator_model_file):
                return os.path.join(directory, file_name)
        return os.path.join(directory, indicator_model_file)

    @staticmethod
    def get_directory(indicator_file):
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        indicator_file = indicator_file.replace(".csv", "").replace("-", "_")
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        return os.path.join(directory, "static/{0}".format(indicator_file))
