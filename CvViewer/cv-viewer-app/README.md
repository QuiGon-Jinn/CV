# cv-viewer-app

This is a minimal manual scaffold for an Angular standalone app with routing.

Next steps (on Windows):

1. Install Node.js (adds `node`, `npm`, and `npx`).

2. From the workspace root run:

```powershell
cd /d D:\Dev\CV\CvViewer\cv-viewer-app
npm install
# then you can run the Angular CLI via npx
npx -y @angular/cli@latest serve
```

Alternatively, if you prefer the official Angular CLI scaffold (recommended), install Node.js then run from `D:\Dev\CV\CvViewer`:

```powershell
npx -y @angular/cli@latest new cv-viewer-app --routing --standalone
```

This manual scaffold provides `src/main.ts`, a standalone `AppComponent`, `HomeComponent`, and routing configuration.
