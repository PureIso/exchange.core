from flask_restful import Resource
from flask import request

class Home(Resource):
    def get(self):
        return {'about': 'Home Page'}, 201

    def post(self):
        json = request.get_json()
        return {'You sent:': json}, 201
