# Utiliser l'image officielle de SQL Server 2019 sur Ubuntu
FROM mcr.microsoft.com/mssql/server:2019-latest

# Définir les variables d'environnement nécessaires
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=YourStrong!Passw0rd
ENV MSSQL_PID=Developer

# Copie le script dans l'image
COPY init.sql /init.sql

# Lancer SQL Server en arrière-plan, exécuter le script, puis arrêter
# Ouvrir le port SQL Server
EXPOSE 1433

# Démarrer SQL Server et exécuter le script d'initialisation
CMD /bin/bash -c "/opt/mssql/bin/sqlservr & \
    sleep 20 && \
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -i /init.sql && \
    wait"
