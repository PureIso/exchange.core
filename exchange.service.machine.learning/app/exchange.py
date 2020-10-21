import os
import logging
import graypy
from app.config import Config
from celery import Celery
from os.path import join, dirname
from dotenv import load_dotenv
from pathlib import Path

class Exchange():
    root_dir = None
    celery = None
    config = None
    current_training_status = {}
    mi_host = "0.0.0.0"
    mi_port = 5005
    mi_graylog_host = "127.0.0.1"
    mi_graylog_port = 12201
    mi_logger = None
    mi_broker_url = "mongodb://celery:celery@service.mongodb:27017/celery"
    mi_result_backend = "mongodb://celery:celery@service.mongodb:27017/celery"

    def __init__(self):
        path = Path(dirname(__file__))
        self.root_dir = join(path.parent,'.env')
        self.current_training_status = {}
        if self.root_dir is not None:
            # Create .env file path.
            print(self.root_dir)
            # Load file from the path.
            load_dotenv(self.root_dir)
            self.celery = Celery('exchange.machine.learning')
            self.config = Config()
            self.mi_host = os.getenv("MI_HOST")
            self.mi_port = os.getenv("MI_PORT")
        if os.getenv("MI_GRAYLOG_HOST") is not None:
            self.mi_graylog_host = os.getenv("MI_GRAYLOG_HOST")
            self.mi_graylog_port = os.getenv("MI_GRAYLOG_PORT")
            handler = graypy.GELFUDPHandler(self.mi_graylog_host, int(self.mi_graylog_port))
            self.mi_logger = logging.getLogger('mi_logger')
            self.mi_logger.setLevel(logging.ERROR)
            self.mi_logger.setLevel(logging.INFO)
            self.mi_logger.addHandler(handler)
        if os.getenv("MI_BROKER_URL") is not None:
            self.mi_broker_url = os.getenv("MI_BROKER_URL")   
            self.mi_result_backend = os.getenv("MI_RESULT_BACKEND")
            self.celery.conf.update(
                broker_url=self.mi_broker_url,
                result_backend=self.mi_result_backend,
                broker_use_ssl=False,
                authSource='celery',
                authMechanism="SCRAM-SHA-1",
                user = 'celery',
                password = 'celery',
                database_name = 'celery',
                taskmeta_collection = 'celery_taskmeta',
                groupmeta_collection = 'celery_groupmeta',
                max_pool_size = 10,
                options = None
            )
        if os.getenv("MI_GRAYLOG_HOST") is not None:
            self.mi_logger.info('Exchange loading')
        super(Exchange, self).__init__()