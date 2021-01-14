import os
import sys
from app.tasks.task_work import training
from flask_restful import Resource, reqparse
from flask import json, Response, request

class RecurrentNeuralNetwork(Resource):
    def __init__(self,exchange):
        self.exchange = exchange
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('hourly', type=str, location='json')
        self.reqparse.add_argument('save', type=str, location='json')
        super(RecurrentNeuralNetwork, self).__init__()

    def get(self):
        try:
            data_files = os.listdir(os.path.join(os.path.dirname(__file__),"static"))
            indicator_files = []
            for file_name in data_files:
                if file_name.endswith(".csv"):
                    indicator_files.append(file_name)

            message = json.dumps({
                "api_version": "1.0.0", 
                "indicator_files": indicator_files})
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
            self.exchange.mi_logger.info("Recurrent Nural Netowork Processing: Target: {hourly} Save: {save}")

            if hourly != None and save != None:
                task = training.delay(hourly, save)
                message = json.dumps(
                    {"task_id": str(task.task_id),
                     "status": str(task.status),
                     "status url": str(request.url_root + 'api/v1/taskstatus?task_id=' + task.task_id)})
                self.exchange.mi_logger.info(message)
                response = Response(message,
                                    status=202,  # Status Accepted
                                    mimetype='application/json')
            else:
                message = json.dumps(
                    {
                        "message": "Invalid input save: {save} hourly: {hourly}"
                    })
                self.exchange.mi_logger.info(message)
                response = Response(message, status=200,
                                    mimetype='application/json')
            return response
        except:
            message = json.dumps({"error": str(sys.exc_info())})
            self.exchange.mi_logger.error(message)
            response = Response(message,
                                status=500,  # Status Internal Server Error
                                mimetype='application/json')
        return response
