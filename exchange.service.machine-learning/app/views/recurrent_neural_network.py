import sys
from app.tasks.task_work import training
from flask_restful import Resource, reqparse
from flask import json, Response, request


class RecurrentNeuralNetwork(Resource):
    def __init__(self):
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('hourly', type=str, location='json')
        self.reqparse.add_argument('save', type=str, location='json')
        super(RecurrentNeuralNetwork, self).__init__()

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
            args = self.reqparse.parse_args()
            hourly = args['hourly']
            save = args['save']

            if hourly != None and save != None:
                task = training.delay(hourly, save)
                message = json.dumps(
                    {"task_id": str(task.task_id),
                     "status": str(task.status),
                     "status url": str(request.url_root + 'api/v1/taskstatus?task_id=' + task.task_id)})
                response = Response(message,
                                    status=202,  # Status Accepted
                                    mimetype='application/json')
            else:
                message = json.dumps(
                    {
                        "message": "Invalid input save: {save} hourly: {hourly}"
                    })
                response = Response(message, status=200,
                                    mimetype='application/json')
            return response
        except:
            message = json.dumps({"error": str(sys.exc_info()[0])})
            response = Response(message,
                                status=500,  # Status Internal Server Error
                                mimetype='application/json')
        return response
