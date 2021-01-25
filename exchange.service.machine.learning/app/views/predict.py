import sys
from app.tasks.task_work import training
from flask_restful import Resource, reqparse
from flask import json, Response, request

class Predict(Resource):
    def __init__(self,exchange):
        self.exchange = exchange
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('indicator_file', type=str, location='json')
        self.reqparse.add_argument('predict', type=bool, location='json')
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
            args = self.reqparse.parse_args()
            indicator_file = args['indicator_file']
            predict = args['predict']
            self.exchange.mi_logger.info("Recurrent Nural Netowork Processing: Target: {0}".format(indicator_file))

            if indicator_file != None:
                task = training.delay(indicator_file, False, predict)
                message = json.dumps(
                    {"task_id": str(task.task_id),
                     "status": str(task.status),
                     "status url": str(request.url_root + 'machinelearning/api/v1/taskstatus?task_id=' + task.task_id)})
                self.exchange.mi_logger.info(message)
                response = Response(message,
                                    status=202,  # Status Accepted
                                    mimetype='application/json')
            else:
                message = json.dumps({
                    "message": "Invalid input target: {0}".format(indicator_file)})
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
