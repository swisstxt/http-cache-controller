# k8s http-cache-controller

a simple http cache layer which routes traffic trough an nginx reverse proxy with caching

## Installation

```
kubectl apply -f https://raw.githubusercontent.com/swisstxt/http-cache-controller/master/service-account.yaml
kubectl apply -f https://raw.githubusercontent.com/swisstxt/http-cache-controller/master/controller-deployment.yaml
kubectl apply -f https://raw.githubusercontent.com/swisstxt/http-cache-controller/master/cache-deployment.yaml
```

## Usage

annotate your service with the following annotations:

```
metadata:
  annotations:
    swisstxt.ch/http-cache-enabled: "true"
```

update your ingress to use the cached service

before:

```
spec:
  rules:
  - host: mydomain.com
    http:
      paths:
      - backend:
          service:
            name: myservicename
            port:
              name: http
```

after:

```
spec:
  rules:
  - host: mydomain.com
    http:
      paths:
      - backend:
          service:
            name: myservicename-cached
            port:
              name: http
```

HAPPY CACHING!

