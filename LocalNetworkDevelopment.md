# Local Network Development

Run both the API and Vite dev server on all network interfaces:

## VS Code

Open the Run and Debug panel and start:

```text
Full Stack (Network)
```

This starts the API on `0.0.0.0:5012` and Vite on `0.0.0.0:5173`.

Stopping the debug session stops the API and frontend dev server. Closing the VS Code-launched debug browser also stops the app. Browsers on other devices, such as an iPhone, cannot signal VS Code when they close, so stop the VS Code debug session when you are done testing from those devices.

## Terminal

```bash
dotnet run --project src/GardenCompanion.Api --launch-profile network
```

```bash
cd frontend
npm run dev:host
```

Find your Mac's local IP address:

```bash
ipconfig getifaddr en0
```

Then open the frontend from another device on the same network:

```text
http://<your-mac-ip>:5173
```

The frontend sends API requests to the same Vite origin (`/api`). During development, Vite proxies those requests to `http://127.0.0.1:5012` on your Mac. This keeps mobile browsers from needing direct cross-origin API access.

If macOS asks about accepting incoming network connections for `dotnet` or `node`, allow it for your private network.
