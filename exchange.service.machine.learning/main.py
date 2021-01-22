import os
from flask_restful import Api
from flask import Flask, request
from app.exchange import Exchange
from app.views.home import Home
from app.views.predict import Predict
from app.views.task_status import TaskStatus
from app.views.recurrent_neural_network import RecurrentNeuralNetwork

if __name__ == '__main__':
    exchange = Exchange()
    application = Flask(__name__, static_url_path='/static')
    api = Api(application)
    api.add_resource(Home, '/')
    api.add_resource(RecurrentNeuralNetwork, '/api/v1/rnn/',resource_class_kwargs={'exchange': exchange})
    api.add_resource(TaskStatus, '/api/v1/taskstatus/',resource_class_kwargs={'exchange': exchange})
    api.add_resource(Predict, '/api/v1/predict/',resource_class_kwargs={'exchange': exchange})
    exchange.mi_logger.info('Machine Learning Service Starting')
    application.run(host=exchange.mi_host, port=exchange.mi_port, debug=False)