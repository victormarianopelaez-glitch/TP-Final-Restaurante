import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'
import AdminView from '../views/AdminView.vue'
import MozoView from '../views/MozoView.vue'
import InformesView from '../views/InformesView.vue'
import PerfilView from '../views/PerfilView.vue'

const routes = [
    { path: '/', redirect: '/login' },
    { path: '/login', name: 'Login', component: Login, meta: { guestOnly: true } },
    { path: '/mozo', name: 'Mozo', component: MozoView, meta: { requiresAuth: true, roles: ['Mozo', 'Administrador'] } },
    { path: '/admin', name: 'Admin', component: AdminView, meta: { requiresAuth: true, roles: ['Administrador'] } },
    { path: '/informes', name: 'Informes', component: InformesView, meta: { requiresAuth: true, roles: ['Mozo', 'Administrador'] } },
    { path: '/perfil', name: 'Perfil', component: PerfilView, meta: { requiresAuth: true, roles: ['Mozo', 'Administrador'] } }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

router.beforeEach((to, from, next) => {
    const token = localStorage.getItem('token')
    const rawUser = localStorage.getItem('usuario')
    let usuario = null
    try {
        usuario = rawUser ? JSON.parse(rawUser) : null
    } catch {
        usuario = null
    }

    if (to.meta.requiresAuth && (!token || !usuario)) {
        return next('/login')
    }

    if (to.meta.guestOnly && token && usuario) {
        return next(usuario.rol === 'Administrador' ? '/admin' : '/mozo')
    }

    if (to.meta.roles && usuario && !to.meta.roles.includes(usuario.rol)) {
        return next('/mozo')
    }

    next()
})

export default router
