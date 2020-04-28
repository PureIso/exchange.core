'use strict';

const electron = require('electron');
const url = require('url');
const path = require('path');
const config = require(path.join(__dirname, 'package.json'));
const { app, BrowserWindow } = electron;

let mainWindow = null;
app.setName(config.productName);
app.on('window-all-closed', () => {
    app.quit()
});

//Listen for application to be ready
app.on('ready', () => {
    //create new window
    mainWindow = new BrowserWindow({
        title: config.productName,
        show: false,
        titleBarStyle: 'hidden',
        frame: false,
        width: 660,
        height: 800,
        icon: path.join(__dirname, 'img/favicon.png'),
        webPreferences: {
            nodeIntegration: true,
            defaultEncoding: 'UTF-8'
        }
    });
    //Load html into main window
    mainWindow.loadURL(url.format({
        pathname: path.join(__dirname, 'index.html'),
        protocol: 'file',
        slashes: true
    }));
    //Open Development Tool
    //mainWindow.webContents.openDevTools()
    //Display UI only when everything is ready
    mainWindow.once('ready-to-show', () => {
        mainWindow.setMenu(null)
        mainWindow.show()
    })
    //Dispose mainwindow object on close
    mainWindow.on('closed', function () {
        mainWindow = null
    })
});