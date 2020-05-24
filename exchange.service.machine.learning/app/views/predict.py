import sys
from flask_restful import Resource, reqparse
from flask import json, Response, request


class Predict(Resource):
    def __init__(self):
        self.reqparse = reqparse.RequestParser()
        super(Predict, self).__init__()

    def get(self):
        try:
            message = json.dumps({"API VERSION": "1.0.0"})
            response = Response(message,
                                status=200,  # Status OK
                                mimetype='application/json')
        except:
            message = json.dumps({"error": str(sys.exc_info()[0])})
            response = Response(message,
                                status=500,  # Status Internal Server Error
                                mimetype='application/json')
        return response

    def post(self):
        try:
            message = json.dumps({"API VERSION": "1.0.0"})
            response = Response(message,
                                status=200,  # Status OK
                                mimetype='application/json')
        except:
            message = json.dumps({"error": str(sys.exc_info()[0])})
            response = Response(message,
                                status=500,  # Status Internal Server Error
                                mimetype='application/json')
        return response
