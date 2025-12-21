# Sample data with k6

Small script with k6 to generate some test data for the demo.
Building the image:
```powershell
docker build . -t local-k6
```

Run the script:
```powershell
docker run --rm local-k6 run k6-test.js
```

Run locally with k6:
```powershell
k6 run k6-test.js
```
