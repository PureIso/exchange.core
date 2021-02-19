"""Main Entrypoint"""
from flask_restful import Api
from flask import Flask
from flask_cors import CORS
from app.configuration import Configuration
from app.views.predict import Predict
from app.views.task_status import TaskStatus
from app.views.recurrent_neural_network import RecurrentNeuralNetwork

if __name__ == '__main__':
    configuration = Configuration()
    application = Flask(__name__, static_url_path='/static')
    # allow CORS for all domains on all routes
    cors = CORS(application, resources={r"/api/v1/*": {"origins": "*"}})
    api = Api(application)
    # route initialization
    rnn_routes = [
        '/',
        '/api/v1/rnn/',
    ]
    api.add_resource(RecurrentNeuralNetwork, *rnn_routes,
                     resource_class_kwargs={'configuration': configuration})
    api.add_resource(TaskStatus, '/api/v1/taskstatus/',
                     resource_class_kwargs={'configuration': configuration})
    api.add_resource(Predict, '/api/v1/predict/',
                     resource_class_kwargs={'configuration': configuration})
    configuration.mi_logger.info('Machine Learning Service Starting')
    # application initialization
    application.run(host=configuration.mi_host, port=configuration.mi_port, debug=False)
