from app.exchange import application

if __name__ == '__main__':
    application.run(host='172.22.0.2', port=5005, debug=True)
