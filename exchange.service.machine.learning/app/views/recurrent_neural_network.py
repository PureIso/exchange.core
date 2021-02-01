import os
import sys
from . import config
from app.tasks.task_work import training
from flask_restful import Resource, reqparse
from flask import json, Response, request

class RecurrentNeuralNetwork(Resource):
    def __init__(self,exchange):
        self.exchange = exchange
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('indicator_file', type=str, location='json')
        self.reqparse.add_argument('save', type=bool, location='json')
        super(RecurrentNeuralNetwork, self).__init__()

    def get(self):
        indicator_files = []
        try:
            indicator_files = config.getFiles()
            message = json.dumps({
                "api_version": "1.0.0", 
                "indicator_files": indicator_files})
            response = Response(message,
                                status=200,  # Status OK
                                mimetype='application/json')
        except:
            message = json.dumps({
                "error": str(sys.exc_info()),
                "api_version": "1.0.0", 
                "indicator_files": indicator_files})
            response = Response(message,
                                status=500,  # Status Internal Server Error
                                mimetype='application/json')
        return response

    def post(self):
        try:
            args = self.reqparse.parse_args()
            indicator_file = args['indicator_file']
            save = args['save']
            previous_prices = []
            self.exchange.mi_logger.info("Recurrent Nural Netowork Processing: Target: {0} Save: {1}".format(indicator_file,save))
            print("Recurrent Nural Netowork Processing: Target: {0} Save: {1}".format(indicator_file,save))

            if indicator_file != None and save != None:
                task = training.delay(indicator_file, save, False, [])
                message = json.dumps(
                    {"task_id": str(task.task_id),
                     "status": str(task.status),
                     "status url": str(request.url_root + 'api/v1/taskstatus?task_id=' + task.task_id)})
                self.exchange.mi_logger.info(message)
                response = Response(message,
                                    status=202,  # Status Accepted
                                    mimetype='application/json')
            else:
                message = json.dumps({
                    "message": "Invalid input save: {0} target: {1}".format(save,indicator_file)})
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
