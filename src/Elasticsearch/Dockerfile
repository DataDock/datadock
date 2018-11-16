FROM openjdk:8

RUN useradd -ms /bin/bash elastic
USER elastic
WORKDIR /home/elastic
RUN mkdir data && \
	mkdir logs && \
	mkdir backup && \
	curl -L -O -k https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-6.2.2.tar.gz && \
    tar -xvf elasticsearch-6.2.2.tar.gz && \
	rm elasticsearch-6.2.2.tar.gz

COPY config /home/elastic/config
ENV ES_PATH_CONF=/home/elastic/config

WORKDIR /home/elastic/elasticsearch-6.2.2/bin
EXPOSE 9200
CMD ./elasticsearch --E network.host=0.0.0.0
