from flask_restful import Api
from flask import Flask, request

from app.views.home import Home
from app.views.recurrent_neural_network import RecurrentNeuralNetwork
from app.views.predict import Predict
from app.views.task_status import TaskStatus

application = Flask(__name__, static_url_path='/static')
api = Api(application)

api.add_resource(Home, '/')
api.add_resource(RecurrentNeuralNetwork, '/api/v1/rnn/')
api.add_resource(TaskStatus, '/api/v1/taskstatus/')
api.add_resource(TaskStatus, '/api/v1/predict/')
