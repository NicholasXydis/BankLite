FROM nginx:alpine@sha256:8b1e78743a03dbb2c95171cc58639fef29abc8816598e27fb910ed2e621e589a

RUN rm /etc/nginx/conf.d/default.conf

COPY nginx-main.conf /etc/nginx/nginx.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --chown=nginx:nginx . /usr/share/nginx/html/

USER nginx

EXPOSE 8080
ENTRYPOINT ["nginx", "-g", "daemon off;"]
