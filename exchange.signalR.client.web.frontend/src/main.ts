import './polyfills';
import './favicon.ico';
import './index.css';

// Import the core angular services.
import { platformBrowserDynamic } from "@angular/platform-browser-dynamic";

// Import the root module for bootstrapping.
import { AppModule } from "./app/app.module";

platformBrowserDynamic().bootstrapModule(AppModule).then(ref => {
    // Ensure Angular destroys itself on hot reloads.
    if (window['ngRef']) {
        window['ngRef'].destroy();
    }
    window['ngRef'] = ref;

    // Otherise, log the boot error
}).catch(err => console.error(err));