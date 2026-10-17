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
    mi_cors_allowed_origins: list[str] = []
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
        configured_cors_origins: str = os.getenv("MI_CORS_ALLOWED_ORIGINS", "")
        self.mi_cors_allowed_origins = [
            origin.strip()
            for origin in configured_cors_origins.split(",")
            if origin.strip()]
        if any("*" in origin for origin in self.mi_cors_allowed_origins):
            raise ValueError(
                "MI_CORS_ALLOWED_ORIGINS must not contain wildcard origins")
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
    def get_files() -> list[str]:
        """[summary]

        Returns:
            [type]: [description]
        """
        directory: Path = Configuration.get_static_directory()
        if not directory.is_dir():
            return []

        static_directory: Path = directory.resolve()
        return sorted(
            path.name
            for path in directory.iterdir()
            if path.is_file()
            and not path.is_symlink()
            and path.suffix == ".csv"
            and path.resolve().parent == static_directory)

    @staticmethod
    def get_static_directory() -> Path:
        """Return the service directory containing CSV inventory files."""
        return Path(__file__).resolve().parent.parent / "static"

    @staticmethod
    def is_valid_indicator_file(indicator_file: object) -> bool:
        """Return whether the value is an exact CSV inventory basename."""
        if not isinstance(indicator_file, str) or not indicator_file:
            return False
        if "/" in indicator_file or "\\" in indicator_file:
            return False
        return indicator_file in Configuration.get_files()

    @staticmethod
    def get_file(indicator_file: str) -> str | None:
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        if not Configuration.is_valid_indicator_file(indicator_file):
            return None
        static_directory: Path = Configuration.get_static_directory()
        indicator_path: Path = static_directory / indicator_file
        return str(indicator_path)

    @staticmethod
    def get_file_normalized_file(indicator_file: str) -> str:
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        directory: Path = Configuration.get_directory(indicator_file)
        normalizer_file: str = indicator_file[:-4] + "_normalizer.json"
        directory.mkdir(parents=True, exist_ok=True)
        existing_file: Path = directory / normalizer_file
        return str(existing_file)

    @staticmethod
    def get_file_model_file(indicator_file: str) -> str:
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        directory: Path = Configuration.get_directory(indicator_file)
        model_file: str = indicator_file[:-4] + "_model.h5"
        directory.mkdir(parents=True, exist_ok=True)
        existing_file: Path = directory / model_file
        return str(existing_file)

    @staticmethod
    def get_directory(indicator_file: str) -> Path:
        """[summary]

        Args:
            indicator_file ([type]): [description]

        Returns:
            [type]: [description]
        """
        if not Configuration.is_valid_indicator_file(indicator_file):
            raise ValueError("Invalid indicator file")
        directory_name: str = indicator_file[:-4].replace("-", "_")
        return Configuration.get_static_directory() / directory_name
