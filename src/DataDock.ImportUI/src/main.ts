import Vue from "vue";
import App from "./App.vue";
import router from "./router";

Vue.config.productionTip = false;
let el = window.document.getElementById("app");
if (el != null) {
  new Vue({
    router,
    render: h => h(App),
    data: {
      ownerId: el.dataset.ownerId,
      publishUrl: el.dataset.publishUrl,
      repoId: el.dataset.repoId,
      baseUrl: el.dataset.baseUrl,
      apiUrl: el.dataset.apiUrl,
      schemaId: el.dataset.schemaId
    }
  }).$mount("#app");
}
