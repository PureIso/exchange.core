import { AppPage } from './app.po';
import { browser } from 'protractor';

describe('App', () => {
    let page: AppPage;

    beforeEach(() => {
        page = new AppPage();
    });

    it('should display landing page title', () => {
        page.navigateToHome();
        browser.waitForAngularEnabled(false);
        browser.sleep(3000);

        let result = 'Exchange.Core';
        page.getTitle().then(function (r) {
            expect(result).toEqual(r);
        });
    });

    // it('should display landing page enter button', () => {
    //     page.navigateToHome();
    //     let result = 'Enter';
    //     page.getLandingPageButton().getText().then(function (r) {
    //         expect(result).toEqual(r);
    //     });
    // });

    // it('should route page when landing page enter button is clicked', () => {
    //     page.navigateToHome();
    //     page.getLandingPageButton().click();
    //     let result = 'Categories';
    //     page.getActiveLink().getText().then(function (r) {
    //         expect(result).toEqual(r);
    //     });
    //     page.getCurrentUrl().then(function (r) {
    //         expect(r.indexOf('/app/categories')).toBeGreaterThan(-1);
    //     });
    // });
});