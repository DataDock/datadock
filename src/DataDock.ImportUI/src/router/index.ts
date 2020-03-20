import Vue from "vue";
import VueRouter from "vue-router";
import Choose from "../views/Choose.vue";
import Define from "../views/Define.vue";
import Upload from "../views/Upload.vue";

Vue.use(VueRouter);

const routes = [
  {
    path: "/",
    name: "choose",
    component: Choose
  },
  {
    path: "/define",
    name: "define",
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    //component: () =>
    //  import(/* webpackChunkName: "about" */ "../views/About.vue")
    component: Define
  },
  {
    path: "/upload",
    name: "upload",
    component: Upload
  }
];

const router = new VueRouter({
  routes
});

export default router;
