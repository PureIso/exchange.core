import { browser, by, element, promise } from 'protractor';

export class AppPage {
    navigateToHome(): promise.Promise<any> {
        return browser.get('http://localhost:9000');
    };

    getCurrentUrl() {
        return browser.getCurrentUrl();
    }

    getTitle(): promise.Promise<string> {
        return browser.getTitle();
    };

    getLandingPageButton() {
        return element(by.className('btn btn-lg landing-btn-enter'));
    }

    getActiveLink() {
        return element(by.className('nav-link py-1 active'));
    }
}
