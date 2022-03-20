# OpenStatusPage
An open source, self hosted status page and monitoring system with options for redundancy and scaleable deployments  

# Quickstart
Either run the `docker` command below direclty or use `docker-compose` to set up a container.  
Then browse to [http://localhost:8181](http://localhost:8181) and you should see the default status page after a moment.  
You can log into the dashboard by clicking the link on the bottom left of the page or by going here [http://localhost:8181/dashboard](http://localhost:8181/dashboard)

Docker
```
docker run -it -p 8181:80 -v data:/data ghcr.io/openstatuspage/openstatuspage \
  --Storage:Driver="sqlite" \
  --Storage:ConnectionString="Data Source=/data/local.db" \
  --ApiKey="9a3dcef7-e0bc-4e30-98bc-b325f5866490" \
  --Tags="demo" \
  --Endpoint="http://localhost:8181"
```

Docker Compose `docker-compose.yml`
```
version: "3"
services:
  openstatuspage:
    image: 'ghcr.io/openstatuspage/openstatuspage'
    restart: unless-stopped
    ports:
     - "8181:80"

    environment:
      "Storage__Driver": "sqlite"
      "Storage__ConnectionString": "Data Source=/data/local.db"
      "ApiKey": "9a3dcef7-e0bc-4e30-98bc-b325f5866490"
      "Tags": "demo"
      "Endpoint": "http://localhost:8181"
      
    volumes:
      - ./data:/data
```
