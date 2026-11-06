<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { API_BASE_URL } from '../api/axios'

const router = useRouter()
const usuario = ref(null)

const cargarUsuario = () => {
  try {
    const raw = localStorage.getItem('usuario')
    usuario.value = raw ? JSON.parse(raw) : null
  } catch {
    usuario.value = null
  }
}

onMounted(() => {
  cargarUsuario()
  window.addEventListener('storage-user-updated', cargarUsuario)
})

const cerrarSesion = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('usuario')
  router.push('/login')
}
</script>

<template>
  <header v-if="usuario" style="background: #0f172a; color: #fff; padding: 12px 24px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px;">
    <div style="display: flex; align-items: center; gap: 20px;">
      <strong style="font-size: 1.15rem; color: #38bdf8;">🍽️ RestoGest</strong>
      <nav style="display: flex; gap: 10px; flex-wrap: wrap;">
        <router-link to="/mozo" class="btn btn-sm btn-secondary">Salón y Comandas</router-link>
        <router-link v-if="usuario.rol === 'Administrador'" to="/admin" class="btn btn-sm btn-secondary">Administración (ABM)</router-link>
        <router-link to="/informes" class="btn btn-sm btn-secondary">Informes</router-link>
        <router-link to="/perfil" class="btn btn-sm btn-secondary">Mi Perfil</router-link>
      </nav>
    </div>

    <div style="display: flex; align-items: center; gap: 12px;">
      <img
        v-if="usuario.avatarRuta"
        :src="`${API_BASE_URL}${usuario.avatarRuta}`"
        alt="Avatar"
        style="width: 34px; height: 34px; border-radius: 50%; object-fit: cover; border: 2px solid #38bdf8;"
      />
      <div v-else style="width: 34px; height: 34px; border-radius: 50%; background: #334155; display: flex; align-items: center; justify-content: center; font-weight: bold; font-size: 0.85rem;">
        {{ usuario.nombre?.[0] || 'U' }}
      </div>
      <div style="font-size: 0.85rem;">
        <div><strong>{{ usuario.nombre }} {{ usuario.apellido }}</strong></div>
        <span class="badge" :class="usuario.rol === 'Administrador' ? 'badge-warning' : 'badge-info'">{{ usuario.rol }}</span>
      </div>
      <button @click="cerrarSesion" class="btn btn-sm btn-danger">Salir</button>
    </div>
  </header>
</template>
