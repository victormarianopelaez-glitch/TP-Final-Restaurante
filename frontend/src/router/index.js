import { createRouter, createWebHistory } from 'vue-router'

import Login from '../views/Login.vue'
import AdminView from '../views/AdminView.vue'
import MozoView from '../views/MozoView.vue'

const routes = [
    {path: '/', redirect: '/login'},
    {path: '/login', name: 'Login', component: Login},
    {path: '/admin', name: 'Admin', component: AdminView},
    {path: '/mozo', name: 'Mozo', component: MozoView}
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router