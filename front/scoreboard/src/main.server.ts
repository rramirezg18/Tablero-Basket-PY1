import { bootstrapApplication } from '@angular/platform-browser'; // <- ojo, platform-browser
import { AppComponent } from './app/app';
// O bien importas el nombre real que exporta tu app.config.server.ts:
import { config as appConfig } from './app/app.config.server';
import 'zone.js/node';


const bootstrap = () => bootstrapApplication(AppComponent, appConfig);
export default bootstrap;
