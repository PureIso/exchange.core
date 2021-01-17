import os


class Config():
    NORMALISEJSONFILENAME = 'normalizer.json'
    MODELH5FILENAME = 'model.h5'
    DAILYCSVFILENAME = "coinbase_btc_eur_historic_data.csv"
    HOURLYCSVFILENAME = "coinbase_btc_eur_historic_data_hourly.csv"

    def getFiles(self):
        indicator_files = []
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        directory = os.path.join(directory,"static")
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(".csv"):
                indicator_files.append(file_name)
        return indicator_files

    def getFile(self, indicator_file):
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        directory = os.path.join(directory,"static")
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(indicator_file):
                return os.path.join(directory,file_name)
        return None

    def getFileNormalizedFile(self, indicator_file):
        indicator_file = indicator_file.replace(".csv", "_normalizer.json")
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        directory = os.path.join(directory,"static")
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(indicator_file):
                return os.path.join(directory,file_name)
        return os.path.join(directory,indicator_file)
    
    def getFileModelFile(self, indicator_file):
        indicator_file = indicator_file.replace(".csv", "_model.h5")
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        directory = os.path.join(directory,"static")
        data_files = os.listdir(directory)
        for file_name in data_files:
            if file_name.endswith(indicator_file):
                return os.path.join(directory,file_name)
        return os.path.join(directory,indicator_file)

    def getDirectory(self):
        os.chdir(os.path.dirname(__file__))
        os.chdir("..")
        directory = os.getcwd()
        return os.path.join(directory,"static")

    def getDailyDirectory(self):
        return "static/daily_data"

    def getHourlyDirectory(self):
        return "static/hourly_data"

    def getHourlyCSVFILE(self):
        return os.path.join(self.getHourlyDirectory(), self.HOURLYCSVFILENAME)

    def getHourlyNormalisJSONFILE(self):
        return os.path.join(self.getHourlyDirectory(), self.NORMALISEJSONFILENAME)

    def getHourlyModelH5FILE(self):
        return os.path.join(self.getHourlyDirectory(), self.MODELH5FILENAME)

    def getDailyCSVFILE(self):
        return os.path.join(self.getDailyDirectory(), self.DAILYCSVFILENAME)

    def getDailyNormalisJSONFILE(self):
        return os.path.join(self.getDailyDirectory(), self.NORMALISEJSONFILENAME)

    def getDailyModelH5FILE(self):
        return os.path.join(self.getDailyDirectory(), self.MODELH5FILENAME)
