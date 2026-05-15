

 Arkitektur principper i praksis
 Opgavesæt M7.03 - Intro til Kubenetes med
minikube
## Formål
Dette opgavesæt har til formål at etablere et udviklingsmiljø på jeres lokale labtop, som er baseret
på Kubernetes.
Vi anvender en local udgave af Kubernetes som er samlet i en pakkeløsning kaldet : minikube.
Lær mere om minikube på deres site  her.
 Opgave A - Installation af Minikube
- Hent minikube software:
Gå til minikube's  Get Started side og følg instrukserne for din platform i trin nr. 1, for at
hente minikube ned til din labtop.
- Fra en terminal, start et minikube cluster på din maskine kør kommandoen:
minikube start
## .
- Tjek at dit cluster er oppe at køre med
kubectl get po -A

 Opgave B - minikube Dashboard
Fra terminalen, start minikube's dashboard med kommandoen
minikube dashboard
. Dette åbner
et Kubernetes dashboard som giver dig overblik over dit lokale k8s-cluster.
Det er ret tomt p.t. så vend tilbage hertil når du har kørt næste opgave.
## Docker Desktop
Sørg for at starte Docker Desktop Application, da minikube installationsprogrammet gerne vil
bruge den.
## 
Opgavesæt M7.03 - Intro til Minikube
## Side 1/4

 Opgave C - Direkte deployment
Find trin 4 (Deploy application) på minikubes
## Get Started
side. Følg instrukserne under
"Service"-fanen (vi gemmer LoadBalancer & Ingress eksemplerne til senere!).
Når du har afprøvet
echo-serveren
, så tilgå dashboard'et igen, og find den oprettede pod og
service og undersøg den tilgængelige information.
 Opgave D - Deployment med YAML-fil
- Start med at oprette en ny tom folder til den nye deployment-fil.
- I folderen, opret en ny fil, kald den f.eks.
deployment.yaml
og indsæt teksten herunder:
intro
I denne opgave oprettes en K8s service, baseret på en enkelt container.
Der anvendes den direkte metode til deployment -- altså kun med anvendelse af
kommandoer fra en terminal.
## 
## Information
hvad er forskellen på kommandoerne:

kubectl create deployment ...
og
kubectl expose deployment ...
## ?
Kan du finde den interne IP adresse for servicen? Hvad med pod'ens IP adresse?
Hvor finder du containerne for pod'en?
Hvordan tilgår du loggen for pod'en?
Hvilket namespace befinder pod'en sig i?
## 
intro
I denne opgave anvendes en yaml-fil som beskriver hvordan vi ønsker at udføre vores
deployment.
Bemærk: Vi opretter 2 instanser (replicas) af vores pod.
## 
Opgavesæt M7.03 - Intro til Minikube
## Side 2/4

- Gennemgå filens indhold og dobbelttjek at indenteringen er korrekt!
- Åben en terminal i folderen
- Opret et nyt deployment med kommandoen
kubectl apply -f deployment.yaml
- Tjek at operationen gik godt, med:
kubectl get deployments hello-world
-- som bør vise at
der er 2 Pods (under READY).
- Vis også en mere detaljeret beskrivelse af operationen med
kubectl describe deployments hello-world
- Lav nu en K8s service som samler de 2 Pods fra forgående operation med flg. kommando:
- Vis lidt info omkring den nye service:

kubectl get services hello-world-service
- Til sidst mangler vi at lave en forbindelse imellem servicen og vores egen maskine, således at
vi kan tilgå den i en browser. Dette klares af minikube med kommandoen:
minikube service hello-world-service --url
. Du får en url tilbage i svaret fra kommandoen,
som du kan tilgå med en browser eller

curl
## -kommandoen.
apiVersion: apps/v1
kind: Deployment
metadata:
name: hello-world
spec:
selector:
matchLabels:
run: load-balancer-example
replicas: 2
template:
metadata:
labels:
run: load-balancer-example
spec:
containers:
-name: hello-world
image: us-docker.pkg.dev/google-samples/containers/gke/hello-app:2.0
ports:
-containerPort: 8080
protocol: TCP
## 1
## 2
## 3
## 4
## 5
## 6
## 7
## 8
## 9
## 10
## 11
## 12
## 13
## 14
## 15
## 16
## 17
## 18
## 19
## 20
kubectl expose deployment hello-world --type=NodePort --name=hello-world-service
## 1
## 2
## 3
Opgavesæt M7.03 - Intro til Minikube
## Side 3/4

##  Opgave E - Test Load-balanceren
Kubernetes har lagt en load balancer ind foran de 2 containers som ligger i de 2 Pods som udgør
servicen oprettet i opgave D. For at tjekke at der bliver vekslet imellem de 2 containers kør
følgende bash-kommando:
##  Opgave F - Cleanup
Til sidst fjerner vi de oprettede Kubernetes objekter igen. Først servicen efterfulgt af deployment:
+ +Ekstra - Din egen service
Har du en microservice liggende i DockerHub, f.eks. din søgemaskine, så lav en deployment og en
service i Kubernetes. For at tjekke load-balanceren skal du have et API-endepunkt med version og
IP adresse.
## ⏹
for i in {1..20}; do curl -s <url fra opgave D.10> | grep Hostname; done
## 1
kubectl delete -n   default service hello-world-service
kubectl delete deployment hello-world
## 1
## 2
Opgavesæt M7.03 - Intro til Minikube
## Side 4/4