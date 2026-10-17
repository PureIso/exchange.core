"""Celery Task Status"""
from flask_restful import Resource, reqparse
from flask import Response, json
from typing import Any
from app.tasks.task_work import training


_FAILURE_STATUS: dict[str, object] = {
    "current_progress": 0,
    "total_progress": 100,
    "status": "Task failed.",
}
_REVOKED_STATUS: dict[str, object] = {
    "current_progress": 0,
    "total_progress": 100,
    "status": "Task revoked.",
}


def _metadata_as_dict(metadata: object) -> dict[str, object] | None:
    if isinstance(metadata, dict):
        return metadata
    if isinstance(metadata, str):
        try:
            decoded_metadata: Any = json.loads(metadata)
        except (TypeError, ValueError):
            return None
        return decoded_metadata if isinstance(decoded_metadata, dict) else None
    return None


def _progress_metadata(metadata: object) -> dict[str, object]:
    status: dict[str, object] | None = _metadata_as_dict(metadata)
    if status is None:
        return {
            "current_progress": 0,
            "total_progress": 100,
            "status": "Task in progress.",
        }
    status = dict(status)
    if "progress" in status and "current_progress" not in status:
        status["current_progress"] = status.pop("progress")
    status.setdefault("current_progress", 0)
    status.setdefault("total_progress", 100)
    status.setdefault("status", "Task in progress.")
    return status


class TaskStatus(Resource):
    """Task Status class
    Args:
        Resource (Resource): RESTFul resource
    """
    def __init__(self, configuration):
        self.configuration = configuration
        self.reqparse = reqparse.RequestParser()
        self.reqparse.add_argument('task_id', type=str, location='json')
        super(TaskStatus, self).__init__()

    def post(self) -> Response:
        """Task Status [post] method
        Returns:
            Response: The response
        """
        args: Any = self.reqparse.parse_args()
        task_id: Any = args['task_id']
        task: Any = self.configuration.celery.AsyncResult(
            id=task_id, app=training)
        if task.state == 'PENDING':
            # job did not start yet
            message: str = json.dumps({
                "state": task.state,
                "status": {}})
        elif task.state == 'FAILURE':
            message = json.dumps({
                "state": task.state,
                "status": _FAILURE_STATUS})
        elif task.state == 'REVOKED':
            message = json.dumps({
                "state": task.state,
                "status": _REVOKED_STATUS})
        elif task.state == 'PROGRESS':
            message = json.dumps({
                "state": task.state,
                "status": _progress_metadata(task.info)})
        else:
            status: dict[str, object] | None = _metadata_as_dict(task.info)
            if status is None:
                status = {
                    "current_progress": 0,
                    "total_progress": 100,
                    "status": "Task status unavailable.",
                }
            message = json.dumps({
                "state": task.state,
                "status": status})

        self.configuration.mi_logger.info(message)
        response: Response = Response(message,
                            status=200,  # Status OK
                            mimetype='application/json')
        return response
