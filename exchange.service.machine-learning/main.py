from flask_restful import Api
from flask import Flask, request
from celery import Celery
from app.home.home import Home
from app.neural_network.recurrent_neural_network import RecurrentNeuralNetwork

application = Flask(__name__, static_url_path='/app/static')
# set the project root directory as the static folder, you can set others.
print(application.name)
celery = Celery(application.name, broker='pyamqp://guest@localhost//')
api = Api(application)

api.add_resource(Home, '/')
api.add_resource(RecurrentNeuralNetwork, '/api/v1/rnn/')

if __name__ == '__main__':
    application.run(host='localhost', port=5005, debug=True)
