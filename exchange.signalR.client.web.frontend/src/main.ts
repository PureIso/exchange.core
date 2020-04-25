import './polyfills';
import './favicon.ico';
import './index.css';

// Import the core angular services.
import { platformBrowserDynamic } from "@angular/platform-browser-dynamic";

// Import the root module for bootstrapping.
import { AppModule } from "./app/app.module";

platformBrowserDynamic().bootstrapModule(AppModule);