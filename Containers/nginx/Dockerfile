FROM frapsoft/openssl AS cert

RUN openssl req -outform PEM -out localhost.crt -verify -keyform PEM -keyout localhost.key -nodes -x509 -batch

FROM nginx AS final

COPY --from=cert /localhost.key /etc/nginx/localhost.key
COPY --from=cert /localhost.crt /etc/nginx/localhost.crt

COPY ./html /usr/share/nginx/html

COPY ./configs/nginx.conf /etc/nginx/nginx.conf