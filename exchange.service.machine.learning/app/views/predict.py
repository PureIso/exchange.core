"""Predict API Controller"""
from flask_restful import Resource, reqparse
from flask import json, Response
from app.tasks.task_work import prediction


class Predict(Resource):
    """Predict API Class
    Args:
        Resource (Resource): RESTFul resource
    """

    def __init__(self, configuration):
        self.configuration = configuration
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('indicator_file', type=str, location='json')
        super(Predict, self).__init__()

    @staticmethod
    def get():
        """Predict [get] method
        Returns:
            Response: The response
        """
        message = json.dumps({"API VERSION": "1.0.0"})
        response = Response(message,
                            status=200,  # Status OK
                            mimetype='application/json')
        return response

    def post(self):
        """predict [post] method
        Returns:
            Response: The response
        """
        args = self.reqparse.parse_args()
        indicator_file = args['indicator_file']
        task = prediction.delay(indicator_file)
        message = json.dumps({
            "task_id": str(task.task_id),
            "status": str(task.status)})

        self.configuration.mi_logger.info(message)
        response = Response(message,
                            status=202,  # Status Accepted
                            mimetype='application/json')
        return response
