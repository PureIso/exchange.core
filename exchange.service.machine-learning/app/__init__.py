from flask_restful import Resource, Api
from flask import Flask, request, send_from_directory
from .rnn import RecurrentNeuralNetworkHourly

# set the project root directory as the static folder, you can set others.
app = Flask(__name__, static_url_path='')
api = Api(app)


class Home(Resource):
    def get(self):
        return {'about': 'Home Page'}

    def post(self):
        json = request.get_json()
        return {'You sent:': json}, 201


class Calculate(Resource):
    def get(self, num):
        return {'result:': num*10}


api.add_resource(Home, '/')
api.add_resource(Calculate, '/api/v1/calculate/<int:num>')
api.add_resource(RecurrentNeuralNetworkHourly, '/api/v1/rnn/')
