<script setup>
import { ref, onMounted } from 'vue'
import api, { API_BASE_URL } from '../api/axios'

const usuario = ref(JSON.parse(localStorage.getItem('usuario') || '{}'))
const mesas = ref([])
const pedidoActivo = ref(null)
const medioPagoSeleccionado = ref('Efectivo')
const mensaje = ref('')
const error = ref('')

// Búsqueda Asíncrona AJAX de Productos y Categorías (Requerimiento 6)
const busquedaTexto = ref('')
const idCategoriaFiltro = ref('')
const categoriasDisponibles = ref([])
const productosEncontrados = ref([])
const cantidadSeleccionada = ref(1)
let debounceTimer = null

// Historial de Pedidos con Paginado por Servidor (Requerimiento 5)
const pedidosHistorial = ref([])
const filtroEstadoPedido = ref('')
const paginaActual = ref(1)
const totalPaginas = ref(1)
const totalRegistros = ref(0)

const limpiarAlertas = () => {
  mensaje.value = ''
  error.value = ''
}

const cargarSalon = async () => {
  try {
    const { data } = await api.get('/mesas')
    mesas.value = data
  } catch (err) {
    error.value = 'No se pudo cargar el estado del salón.'
  }
}

const cargarCategorias = async () => {
  try {
    const { data } = await api.get('/categorias?all=true')
    categoriasDisponibles.value = data
  } catch {}
}

// Búsqueda Asíncrona en el Servidor vía AJAX a medida que el mozo tipea
const buscarProductosAsync = () => {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(async () => {
    try {
      const params = new URLSearchParams()
      if (busquedaTexto.value.trim()) params.append('q', busquedaTexto.value.trim())
      if (idCategoriaFiltro.value) params.append('idCategoria', idCategoriaFiltro.value)
      const { data } = await api.get(`/productos/buscar?${params.toString()}`)
      productosEncontrados.value = data
    } catch {
      productosEncontrados.value = []
    }
  }, 250)
}

const abrirMesa = async (mesa) => {
  limpiarAlertas()
  try {
    const { data } = await api.post('/pedidos/abrir', { idMesa: mesa.idMesa })
    mensaje.value = `Mesa #${mesa.numero} abierta correctamente (Pedido #${data.idPedido}).`
    await cargarSalon()
    await seleccionarPedido(data.idPedido)
    await cargarHistorialPedidos(1)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'No se pudo abrir la mesa.'
  }
}

const seleccionarPedido = async (idPedido) => {
  limpiarAlertas()
  try {
    const { data } = await api.get(`/pedidos/${idPedido}`)
    pedidoActivo.value = data
    buscarProductosAsync()
  } catch (err) {
    error.value = 'Error al obtener el detalle de la comanda.'
  }
}

const agregarProductoAComanda = async (prod) => {
  if (!pedidoActivo.value) return
  limpiarAlertas()
  try {
    const { data } = await api.post(`/pedidos/${pedidoActivo.value.idPedido}/detalles`, {
      idProducto: prod.idProducto,
      cantidad: Number(cantidadSeleccionada.value) || 1
    })
    pedidoActivo.value = data
    cantidadSeleccionada.value = 1
    await cargarSalon()
    await cargarHistorialPedidos(paginaActual.value)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'Error al agregar ítem al pedido.'
  }
}

const quitarItemDeComanda = async (idDetalle) => {
  if (!pedidoActivo.value) return
  limpiarAlertas()
  try {
    const { data } = await api.delete(`/pedidos/${pedidoActivo.value.idPedido}/detalles/${idDetalle}`)
    pedidoActivo.value = data
    await cargarSalon()
    await cargarHistorialPedidos(paginaActual.value)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'No se pudo quitar el producto.'
  }
}

const registrarPago = async () => {
  if (!pedidoActivo.value) return
  limpiarAlertas()
  try {
    const { data } = await api.post('/pagos', {
      idPedido: pedidoActivo.value.idPedido,
      medioPago: medioPagoSeleccionado.value
    })
    mensaje.value = data.mensaje
    pedidoActivo.value = null
    await cargarSalon()
    await cargarHistorialPedidos(1)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'No se pudo registrar el pago.'
  }
}

const anularPedidoAdmin = async (idPedido) => {
  if (!confirm(`¿Confirma la ANULACIÓN lógica del Pedido #${idPedido}? Se registrará su usuario Administrador en la auditoría.`)) return
  limpiarAlertas()
  try {
    const { data } = await api.post(`/pedidos/${idPedido}/anular`)
    mensaje.value = data.mensaje
    if (pedidoActivo.value?.idPedido === idPedido) {
      pedidoActivo.value = null
    }
    await cargarSalon()
    await cargarHistorialPedidos(paginaActual.value)
  } catch (err) {
    error.value = err.response?.data?.mensaje || 'No tiene permisos o ocurrió un error al anular.'
  }
}

const cargarHistorialPedidos = async (page = 1) => {
  try {
    const params = new URLSearchParams({ page, pageSize: 6 })
    if (filtroEstadoPedido.value) params.append('estado', filtroEstadoPedido.value)
    const { data } = await api.get(`/pedidos?${params.toString()}`)
    pedidosHistorial.value = data.items
    paginaActual.value = data.page
    totalPaginas.value = data.totalPages || 1
    totalRegistros.value = data.totalItems
  } catch {}
}

onMounted(() => {
  cargarSalon()
  cargarCategorias()
  cargarHistorialPedidos(1)
})
</script>

<template>
  <div class="container">
    <h1 style="margin-bottom: 14px;">Salón de Mesas y Gestión de Comandas</h1>

    <div v-if="mensaje" class="alert alert-ok">{{ mensaje }}</div>
    <div v-if="error" class="alert alert-error">{{ error }}</div>

    <div class="grid-2">
      <!-- Columna Izquierda: Estado del Salón -->
      <div class="card">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
          <h3>Estado de Mesas en Vivo</h3>
          <button class="btn btn-sm btn-secondary" @click="cargarSalon">🔄 Actualizar</button>
        </div>

        <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(165px, 1fr)); gap: 12px;">
          <div
            v-for="m in mesas"
            :key="m.idMesa"
            style="border: 2px solid; border-radius: 8px; padding: 12px; display: flex; flex-direction: column; justify-content: space-between;"
            :style="{
              borderColor: m.estado === 'Ocupada' ? '#dc2626' : '#16a34a',
              background: m.estado === 'Ocupada' ? '#fef2f2' : '#f0fdf4'
            }"
          >
            <div>
              <div style="display: flex; justify-content: space-between; align-items: center;">
                <strong>Mesa #{{ m.numero }}</strong>
                <span class="badge" :class="m.estado === 'Ocupada' ? 'badge-danger' : 'badge-success'">
                  {{ m.estado }}
                </span>
              </div>
              <div style="font-size: 0.8rem; color: #475569; margin-top: 4px;">
                Capacidad: {{ m.capacidad }} personas
              </div>

              <div v-if="m.pedidoAbierto" style="margin-top: 8px; font-size: 0.8rem; border-top: 1px dashed #fca5a5; padding-top: 6px;">
                <div>Comanda: <strong>#{{ m.pedidoAbierto.idPedido }}</strong></div>
                <div>Mozo: {{ m.pedidoAbierto.mozoNombre }}</div>
                <div>Hace: {{ m.pedidoAbierto.minutosOcupada }} min</div>
                <div>Subtotal: <strong>${{ m.pedidoAbierto.subtotal.toFixed(2) }}</strong></div>
              </div>
            </div>

            <div style="margin-top: 10px;">
              <button
                v-if="m.estado === 'Libre'"
                class="btn btn-sm btn-success"
                style="width: 100%;"
                @click="abrirMesa(m)"
              >
                Abrir Mesa
              </button>
              <button
                v-else-if="m.pedidoAbierto"
                class="btn btn-sm btn-primary"
                style="width: 100%;"
                @click="seleccionarPedido(m.pedidoAbierto.idPedido)"
              >
                Ver / Cargar Comanda
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Columna Derecha: Comanda Seleccionada y Búsqueda Asíncrona AJAX -->
      <div class="card">
        <div v-if="!pedidoActivo" style="text-align: center; padding: 40px 10px; color: #64748b;">
          <h3>Seleccione una mesa ocupada o abra una mesa libre</h3>
          <p style="font-size: 0.88rem; margin-top: 6px;">
            Desde aquí podrá buscar platos vía AJAX, cargarlos a la comanda y registrar el cobro.
          </p>
        </div>

        <div v-else>
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;">
            <div>
              <h3>Comanda #{{ pedidoActivo.idPedido }} — Mesa #{{ pedidoActivo.mesaNumero }}</h3>
              <span style="font-size: 0.82rem; color: #64748b;">Mozo: {{ pedidoActivo.mozoNombre }}</span>
            </div>
            <span class="badge" :class="pedidoActivo.estado === 'Abierto' ? 'badge-warning' : 'badge-success'">
              {{ pedidoActivo.estado }}
            </span>
          </div>

          <!-- Buscador Asíncrono AJAX de Productos (solo si el pedido está Abierto) -->
          <div v-if="pedidoActivo.estado === 'Abierto'" style="background: #f8fafc; padding: 12px; border-radius: 8px; border: 1px solid #e2e8f0; margin-bottom: 14px;">
            <label>Búsqueda Asíncrona de Platos / Bebidas Disponibles (AJAX)</label>
            <div style="display: grid; grid-template-columns: 1fr 150px 80px; gap: 8px; margin-bottom: 10px;">
              <input
                type="text"
                v-model="busquedaTexto"
                @input="buscarProductosAsync"
                placeholder="Tipee nombre del plato (ej. milanesa, agua)..."
              />
              <select v-model="idCategoriaFiltro" @change="buscarProductosAsync">
                <option value="">Todas las categorías</option>
                <option v-for="c in categoriasDisponibles" :key="c.idCategoria" :value="c.idCategoria">
                  {{ c.nombre }}
                </option>
              </select>
              <input type="number" min="1" v-model="cantidadSeleccionada" title="Cantidad" />
            </div>

            <div style="max-height: 170px; overflow-y: auto; border: 1px solid #e2e8f0; border-radius: 6px; background: #fff;">
              <div
                v-for="prod in productosEncontrados"
                :key="prod.idProducto"
                style="display: flex; justify-content: space-between; align-items: center; padding: 7px 10px; border-bottom: 1px solid #f1f5f9; font-size: 0.85rem;"
              >
                <div style="display: flex; align-items: center; gap: 8px;">
                  <img
                    v-if="prod.imagenRuta"
                    :src="`${API_BASE_URL}${prod.imagenRuta}`"
                    style="width: 30px; height: 30px; border-radius: 4px; object-fit: cover;"
                  />
                  <div>
                    <strong>{{ prod.nombre }}</strong>
                    <span style="color: #64748b; font-size: 0.78rem;"> ({{ prod.categoriaNombre }})</span>
                  </div>
                </div>
                <div style="display: flex; align-items: center; gap: 8px;">
                  <strong>${{ prod.precioVigente.toFixed(2) }}</strong>
                  <button class="btn btn-sm btn-primary" @click="agregarProductoAComanda(prod)">+ Agregar</button>
                </div>
              </div>
              <div v-if="productosEncontrados.length === 0" style="padding: 10px; text-align: center; color: #64748b; font-size: 0.82rem;">
                Sin productos disponibles para ese filtro.
              </div>
            </div>
          </div>

          <!-- Detalle de Ítems Cargados en el Pedido -->
          <table class="table">
            <thead>
              <tr>
                <th>Producto</th>
                <th>Cant.</th>
                <th>Precio Hist.</th>
                <th>Subtotal</th>
                <th v-if="pedidoActivo.estado === 'Abierto'">Acción</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="d in pedidoActivo.detalles" :key="d.idDetalle">
                <td>{{ d.productoNombre }}</td>
                <td>{{ d.cantidad }}</td>
                <td>${{ d.precioHistorico.toFixed(2) }}</td>
                <td><strong>${{ d.subtotal.toFixed(2) }}</strong></td>
                <td v-if="pedidoActivo.estado === 'Abierto'">
                  <button class="btn btn-sm btn-danger" @click="quitarItemDeComanda(d.idDetalle)">Quitar</button>
                </td>
              </tr>
              <tr v-if="!pedidoActivo.detalles?.length">
                <td colspan="5" style="text-align: center; color: #64748b;">Aún no se cargaron productos en esta comanda.</td>
              </tr>
            </tbody>
          </table>

          <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 14px; padding-top: 12px; border-top: 2px solid #e2e8f0;">
            <div style="font-size: 1.15rem;">
              Total Cuenta: <strong>${{ (pedidoActivo.total || 0).toFixed(2) }}</strong>
            </div>

            <div v-if="pedidoActivo.estado === 'Abierto'" style="display: flex; gap: 8px; align-items: center;">
              <select v-model="medioPagoSeleccionado" style="width: 150px;">
                <option value="Efectivo">Efectivo</option>
                <option value="Tarjeta">Tarjeta</option>
                <option value="Transferencia">Transferencia</option>
              </select>
              <button class="btn btn-success" @click="registrarPago" :disabled="!pedidoActivo.detalles?.length">
                💳 Cobrar y Cerrar Mesa
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Listado Histórico de Pedidos con Paginado por Servidor (Requerimiento 5) -->
    <div class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; flex-wrap: wrap; gap: 10px;">
        <h3>Historial de Pedidos (Paginado por Servidor)</h3>
        <div style="display: flex; gap: 8px; align-items: center;">
          <select v-model="filtroEstadoPedido" @change="cargarHistorialPedidos(1)" style="width: 180px;">
            <option value="">Todos los estados</option>
            <option value="Abierto">Abierto</option>
            <option value="Pagado">Pagado</option>
            <option value="Anulado">Anulado</option>
          </select>
        </div>
      </div>

      <table class="table">
        <thead>
          <tr>
            <th># Pedido</th>
            <th>Mesa</th>
            <th>Mozo</th>
            <th>Apertura</th>
            <th>Cierre</th>
            <th>Total</th>
            <th>Estado</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in pedidosHistorial" :key="p.idPedido">
            <td><strong>#{{ p.idPedido }}</strong></td>
            <td>Mesa #{{ p.mesaNumero }}</td>
            <td>{{ p.mozoNombre }}</td>
            <td>{{ new Date(p.fechaApertura).toLocaleString() }}</td>
            <td>{{ p.fechaCierre ? new Date(p.fechaCierre).toLocaleString() : '-' }}</td>
            <td><strong>${{ p.total.toFixed(2) }}</strong></td>
            <td>
              <span
                class="badge"
                :class="{
                  'badge-warning': p.estado === 'Abierto',
                  'badge-success': p.estado === 'Pagado',
                  'badge-danger': p.estado === 'Anulado'
                }"
              >
                {{ p.estado }}
              </span>
              <div v-if="p.estado === 'Anulado' && p.adminAnulacionNombre" style="font-size: 0.72rem; color: #991b1b;">
                Por: {{ p.adminAnulacionNombre }}
              </div>
            </td>
            <td style="display: flex; gap: 6px;">
              <button class="btn btn-sm btn-secondary" @click="seleccionarPedido(p.idPedido)">Ver Detalle</button>
              <button
                v-if="usuario.rol === 'Administrador' && p.estado !== 'Anulado'"
                class="btn btn-sm btn-danger"
                @click="anularPedidoAdmin(p.idPedido)"
              >
                Anular (Admin)
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="pagination">
        <span style="font-size: 0.85rem; color: #64748b;">
          Total de registros en servidor: <strong>{{ totalRegistros }}</strong> (Página {{ paginaActual }} de {{ totalPaginas }})
        </span>
        <div style="display: flex; gap: 8px;">
          <button class="btn btn-sm btn-secondary" :disabled="paginaActual <= 1" @click="cargarHistorialPedidos(paginaActual - 1)">
            ← Anterior
          </button>
          <button class="btn btn-sm btn-secondary" :disabled="paginaActual >= totalPaginas" @click="cargarHistorialPedidos(paginaActual + 1)">
            Siguiente →
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
