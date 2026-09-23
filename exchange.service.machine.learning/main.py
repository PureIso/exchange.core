"""Main Entrypoint"""
from flask_restful import Api
from flask import Flask
from flask_cors import CORS
from app.configuration import Configuration
from app.views.predict import Predict
from app.views.task_status import TaskStatus
from app.views.recurrent_neural_network import RecurrentNeuralNetwork


def create_app(configuration=None):
    """Create the Flask application without starting the development server."""
    configuration = configuration or Configuration()
    application = Flask(__name__, static_url_path='/static')
    CORS(application, resources={r"/api/v1/*": {"origins": "*"}})
    api = Api(application)
    api.add_resource(
        RecurrentNeuralNetwork,
        '/',
        '/api/v1/rnn/',
        resource_class_kwargs={'configuration': configuration})
    api.add_resource(
        TaskStatus,
        '/api/v1/taskstatus/',
        resource_class_kwargs={'configuration': configuration})
    api.add_resource(
        Predict,
        '/api/v1/predict/',
        resource_class_kwargs={'configuration': configuration})
    return application


if __name__ == '__main__':
    configuration = Configuration()
    application = create_app(configuration)
    configuration.mi_logger.info('Machine Learning Service Starting')
    # application initialization
    application.run(host=configuration.mi_host, port=configuration.mi_port, debug=False)
