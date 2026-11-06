<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/axios'

const usuario = ref(JSON.parse(localStorage.getItem('usuario') || '{}'))
const tabInforme = ref('porCategoria')

// 1. Productos agrupados por categoría
const filtroDisponibilidad = ref('')
const productosPorCat = ref([])

// 2. Más vendidos en los últimos X días
const diasPlazo = ref(30)
const masVendidos = ref([])

// 3. Mesas ocupadas actualmente
const mesasOcupadas = ref([])

// 4. Recaudación por rango de fechas
const fechaDesde = ref(new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10))
const fechaHasta = ref(new Date().toISOString().slice(0, 10))
const recaudacion = ref({ recaudacionTotal: 0, cantidadTicketsPagados: 0, porMedioPago: [], porMozo: [], detallePagos: [] })

// 5. Auditoría de Anulaciones (Solo Administrador)
const auditoria = ref([])

const cargarPorCategoria = async () => {
  const q = filtroDisponibilidad.value ? `?estado=${encodeURIComponent(filtroDisponibilidad.value)}` : ''
  const { data } = await api.get(`/informes/productos-por-categoria${q}`)
  productosPorCat.value = data
}

const cargarMasVendidos = async () => {
  const { data } = await api.get(`/informes/mas-vendidos?dias=${diasPlazo.value}`)
  masVendidos.value = data.items
}

const cargarMesasOcupadas = async () => {
  const { data } = await api.get('/informes/mesas-ocupadas')
  mesasOcupadas.value = data
}

const cargarRecaudacion = async () => {
  const { data } = await api.get(`/informes/recaudacion?desde=${fechaDesde.value}&hasta=${fechaHasta.value}`)
  recaudacion.value = data
}

const cargarAuditoria = async () => {
  if (usuario.value.rol !== 'Administrador') return
  const { data } = await api.get('/informes/auditoria-anulaciones')
  auditoria.value = data
}

onMounted(() => {
  cargarPorCategoria()
  cargarMasVendidos()
  cargarMesasOcupadas()
  cargarRecaudacion()
  if (usuario.value.rol === 'Administrador') {
    cargarAuditoria()
  }
})
</script>

<template>
  <div class="container">
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; flex-wrap: wrap; gap: 10px;">
      <h1>📊 Informes y Reportes del Restaurante</h1>
      <div style="display: flex; gap: 6px; flex-wrap: wrap;">
        <button class="btn btn-sm" :class="tabInforme === 'porCategoria' ? 'btn-primary' : 'btn-secondary'" @click="tabInforme = 'porCategoria'">
          1. Menú por Categoría
        </button>
        <button class="btn btn-sm" :class="tabInforme === 'masVendidos' ? 'btn-primary' : 'btn-secondary'" @click="tabInforme = 'masVendidos'">
          2. Más Vendidos (X días)
        </button>
        <button class="btn btn-sm" :class="tabInforme === 'mesasOcupadas' ? 'btn-primary' : 'btn-secondary'" @click="tabInforme = 'mesasOcupadas'">
          3. Mesas Ocupadas Ahora
        </button>
        <button class="btn btn-sm" :class="tabInforme === 'recaudacion' ? 'btn-primary' : 'btn-secondary'" @click="tabInforme = 'recaudacion'">
          4. Recaudación por Fechas
        </button>
        <button
          v-if="usuario.rol === 'Administrador'"
          class="btn btn-sm"
          :class="tabInforme === 'auditoria' ? 'btn-danger' : 'btn-secondary'"
          @click="tabInforme = 'auditoria'; cargarAuditoria()"
        >
          5. 🔒 Auditoría de Anulaciones (Admin)
        </button>
      </div>
    </div>

    <!-- INFORME 1: PRODUCTOS AGRUPADOS POR CATEGORÍA -->
    <div v-if="tabInforme === 'porCategoria'" class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px;">
        <h3>1. Productos Agrupados por Categoría</h3>
        <select v-model="filtroDisponibilidad" @change="cargarPorCategoria" style="width: 220px;">
          <option value="">Todos (Disponibles y Sin stock)</option>
          <option value="Disponible">Solo Disponibles</option>
          <option value="Sin stock">Solo Sin stock</option>
        </select>
      </div>

      <div v-for="grupo in productosPorCat" :key="grupo.idCategoria" style="margin-bottom: 18px;">
        <h4 style="background: #f1f5f9; padding: 8px 12px; border-radius: 6px;">
          {{ grupo.categoria }} <span style="font-weight: normal; color: #64748b; font-size: 0.85rem;">({{ grupo.productos.length }} ítems)</span>
        </h4>
        <table class="table">
          <thead>
            <tr>
              <th>Producto</th>
              <th>Descripción</th>
              <th>Precio Vigente</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in grupo.productos" :key="p.idProducto">
              <td><strong>{{ p.nombre }}</strong></td>
              <td>{{ p.descripcion || '-' }}</td>
              <td>${{ p.precioVigente.toFixed(2) }}</td>
              <td>
                <span class="badge" :class="p.estado === 'Disponible' ? 'badge-success' : 'badge-danger'">{{ p.estado }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- INFORME 2: PRODUCTOS MÁS VENDIDOS EN LOS ÚLTIMOS X DÍAS -->
    <div v-if="tabInforme === 'masVendidos'" class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px;">
        <h3>2. Ranking de Productos Más Vendidos</h3>
        <div style="display: flex; gap: 8px; align-items: center;">
          <span>Últimos (días):</span>
          <input type="number" min="1" v-model="diasPlazo" style="width: 90px;" />
          <button class="btn btn-sm btn-primary" @click="cargarMasVendidos">Consultar</button>
        </div>
      </div>

      <table class="table">
        <thead>
          <tr>
            <th>Posición</th>
            <th>Producto</th>
            <th>Categoría</th>
            <th>Unidades Vendidas</th>
            <th>Total Facturado</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(item, idx) in masVendidos" :key="item.idProducto">
            <td><strong>#{{ idx + 1 }}</strong></td>
            <td>{{ item.nombre }}</td>
            <td>{{ item.categoria }}</td>
            <td><span class="badge badge-info">{{ item.cantidadVendida }} u.</span></td>
            <td><strong>${{ item.totalRecaudado.toFixed(2) }}</strong></td>
          </tr>
          <tr v-if="!masVendidos.length">
            <td colspan="5" style="text-align: center; color: #64748b;">No se registran ventas pagadas en el plazo elegido.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- INFORME 3: MESAS OCUPADAS ACTUALMENTE -->
    <div v-if="tabInforme === 'mesasOcupadas'" class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px;">
        <h3>3. Mesas Ocupadas Actualmente (Tiempo y Subtotal en Vivo)</h3>
        <button class="btn btn-sm btn-secondary" @click="cargarMesasOcupadas">🔄 Actualizar</button>
      </div>

      <table class="table">
        <thead>
          <tr>
            <th>Mesa</th>
            <th>Comanda</th>
            <th>Mozo Asignado</th>
            <th>Hora Apertura</th>
            <th>Tiempo Ocupada</th>
            <th>Ítems</th>
            <th>Subtotal Consumido</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in mesasOcupadas" :key="m.idPedido">
            <td><strong>Mesa #{{ m.mesaNumero }}</strong> ({{ m.capacidad }} pers.)</td>
            <td>#{{ m.idPedido }}</td>
            <td>{{ m.mozo }}</td>
            <td>{{ new Date(m.fechaApertura).toLocaleTimeString() }}</td>
            <td><span class="badge badge-warning">{{ m.tiempoFormateado }} ({{ m.minutosOcupada }} min)</span></td>
            <td>{{ m.cantidadItems }}</td>
            <td><strong>${{ m.subtotalConsumido.toFixed(2) }}</strong></td>
          </tr>
          <tr v-if="!mesasOcupadas.length">
            <td colspan="7" style="text-align: center; color: #64748b;">No hay mesas ocupadas en este momento.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- INFORME 4: RECAUDACIÓN TOTAL EN RANGO DE FECHAS -->
    <div v-if="tabInforme === 'recaudacion'" class="card">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; flex-wrap: wrap; gap: 10px;">
        <h3>4. Recaudación Total (Solo Pedidos Pagados Válidos, Excluye Anulados)</h3>
        <div style="display: flex; gap: 8px; align-items: center;">
          <input type="date" v-model="fechaDesde" style="width: 150px;" />
          <span>a</span>
          <input type="date" v-model="fechaHasta" style="width: 150px;" />
          <button class="btn btn-sm btn-primary" @click="cargarRecaudacion">Filtrar</button>
        </div>
      </div>

      <div style="display: flex; gap: 16px; margin-bottom: 16px; flex-wrap: wrap;">
        <div style="background: #f0fdf4; border: 1px solid #86efac; padding: 14px 20px; border-radius: 8px; flex: 1;">
          <div style="font-size: 0.82rem; color: #166534;">Recaudación Neta Válida</div>
          <div style="font-size: 1.5rem; font-weight: bold; color: #15803d;">
            ${{ (recaudacion.recaudacionTotal || 0).toFixed(2) }}
          </div>
        </div>
        <div style="background: #eff6ff; border: 1px solid #93c5fd; padding: 14px 20px; border-radius: 8px; flex: 1;">
          <div style="font-size: 0.82rem; color: #1e40af;">Tickets Pagados Válidos</div>
          <div style="font-size: 1.5rem; font-weight: bold; color: #1d4ed8;">
            {{ recaudacion.cantidadTicketsPagados || 0 }}
          </div>
        </div>
      </div>

      <div class="grid-2">
        <div>
          <h4>Desglose por Medio de Pago</h4>
          <table class="table" style="margin-top: 6px;">
            <thead>
              <tr><th>Medio de Pago</th><th>Pagos</th><th>Subtotal</th></tr>
            </thead>
            <tbody>
              <tr v-for="mp in recaudacion.porMedioPago" :key="mp.medioPago">
                <td>{{ mp.medioPago }}</td>
                <td>{{ mp.cantidadPagos }}</td>
                <td><strong>${{ mp.subtotal.toFixed(2) }}</strong></td>
              </tr>
            </tbody>
          </table>
        </div>
        <div>
          <h4>Métricas de Atención por Mozo</h4>
          <table class="table" style="margin-top: 6px;">
            <thead>
              <tr><th>Mozo</th><th>Pedidos Cerrados</th><th>Total Facturado</th></tr>
            </thead>
            <tbody>
              <tr v-for="mz in recaudacion.porMozo" :key="mz.mozo">
                <td>{{ mz.mozo }}</td>
                <td>{{ mz.pedidosAtendidos }}</td>
                <td><strong>${{ mz.totalFacturado.toFixed(2) }}</strong></td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- INFORME 5: AUDITORÍA DE ANULACIONES (EXCLUSIVO ADMINISTRADOR) -->
    <div v-if="tabInforme === 'auditoria' && usuario.rol === 'Administrador'" class="card">
      <h3>5. 🔒 Listado de Auditoría de Pedidos y Pagos Anulados (Exclusivo Administrador)</h3>
      <p style="font-size: 0.85rem; color: #64748b; margin-bottom: 12px;">
        Registro histórico inalterable de anulaciones lógicas con fecha, importe y administrador responsable.
      </p>

      <table class="table">
        <thead>
          <tr>
            <th>Pedido</th>
            <th>Mesa</th>
            <th>Mozo Original</th>
            <th>Medio Pago</th>
            <th>Monto Anulado</th>
            <th>Fecha Anulación</th>
            <th>Administrador Responsable</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in auditoria" :key="a.idPedido">
            <td><strong>#{{ a.idPedido }}</strong></td>
            <td>Mesa #{{ a.mesaNumero }}</td>
            <td>{{ a.mozoOriginal }}</td>
            <td>{{ a.medioPago }}</td>
            <td><strong style="color: #dc2626;">${{ a.montoAnulado.toFixed(2) }}</strong></td>
            <td>{{ a.fechaAnulacion ? new Date(a.fechaAnulacion).toLocaleString() : '-' }}</td>
            <td><span class="badge badge-danger">{{ a.adminResponsable }}</span></td>
          </tr>
          <tr v-if="!auditoria.length">
            <td colspan="7" style="text-align: center; color: #64748b;">No existen anulaciones registradas.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
