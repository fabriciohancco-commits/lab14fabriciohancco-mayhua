const $ = selector => document.querySelector(selector);
const tbody = $("#product-rows"), form = $("#product-form"), notice = $("#notice");
let products = [], categories = [];
const cash = value => new Intl.NumberFormat("es-PE", { style: "currency", currency: "PEN" }).format(value);
const safe = value => String(value).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
async function api(url, options) {
  const response = await fetch(url, options);
  if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.message || "No se pudo completar la operación."); }
  return response.status === 204 ? null : response.json();
}
async function load() {
  [categories, products] = await Promise.all([api("/api/categories"), api("/api/products")]);
  $("#category-id").innerHTML = categories.map(c => `<option value="${c.id}">${safe(c.name)}</option>`).join("");
  render();
}
function render() {
  const search = $("#search").value.trim().toLocaleLowerCase("es");
  const shown = products.filter(p => `${p.name} ${p.categoryName}`.toLocaleLowerCase("es").includes(search));
  $("#total-products").textContent = products.length;
  $("#total-categories").textContent = categories.length;
  $("#catalog-value").textContent = cash(products.reduce((sum, p) => sum + p.price, 0));
  $("#nav-total").textContent = products.length;
  tbody.innerHTML = shown.map(p => `<tr>
    <td><div class="product-name"><span class="product-glyph">▧</span><span>${safe(p.name)}</span></div></td>
    <td><span class="category-badge">${safe(p.categoryName)}</span></td>
    <td class="price">${cash(p.price)}</td><td class="product-id">PR-${String(p.id).padStart(4, "0")}</td>
    <td><div class="row-actions"><button class="icon-button" title="Editar" aria-label="Editar ${safe(p.name)}" data-edit="${p.id}">✎</button><button class="icon-button" title="Eliminar" aria-label="Eliminar ${safe(p.name)}" data-delete="${p.id}">⌫</button></div></td>
  </tr>`).join("");
  $("#empty").hidden = shown.length > 0;
  $("#table-count").textContent = `Mostrando ${shown.length} de ${products.length} productos`;
}
function resetForm() {
  form.reset(); $("#product-id").value = ""; $("#form-title").textContent = "Registrar producto";
  $("#save").innerHTML = "Guardar producto <span>→</span>"; $("#cancel").hidden = true; notice.hidden = true;
}
function error(message) { notice.textContent = message; notice.hidden = false; }
tbody.addEventListener("click", async event => {
  const edit = event.target.closest("[data-edit]"), remove = event.target.closest("[data-delete]");
  if (edit) {
    const p = products.find(x => x.id === Number(edit.dataset.edit)); if (!p) return;
    $("#product-id").value = p.id; $("#product-name").value = p.name; $("#category-id").value = p.categoryId; $("#price").value = p.price;
    $("#form-title").textContent = "Editar producto"; $("#save").innerHTML = "Guardar cambios <span>→</span>"; $("#cancel").hidden = false;
    $("#editor").scrollIntoView({ behavior: "smooth", block: "center" });
  }
  if (remove) {
    const p = products.find(x => x.id === Number(remove.dataset.delete));
    if (!p || !confirm(`¿Eliminar “${p.name}” del inventario?`)) return;
    try { await api(`/api/products/${p.id}`, { method: "DELETE" }); await load(); } catch (e) { error(e.message); }
  }
});
form.addEventListener("submit", async event => {
  event.preventDefault(); const id = $("#product-id").value;
  const body = { name: $("#product-name").value.trim(), price: Number($("#price").value), categoryId: Number($("#category-id").value) };
  try {
    await api(id ? `/api/products/${id}` : "/api/products", { method: id ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    resetForm(); await load();
  } catch (e) { error(e.message); }
});
$("#search").addEventListener("input", render);
$("#cancel").addEventListener("click", resetForm);
function addProduct() { resetForm(); $("#editor").scrollIntoView({ behavior: "smooth", block: "center" }); $("#product-name").focus({ preventScroll: true }); }
$("#add-new").addEventListener("click", addProduct); $("#empty-add").addEventListener("click", addProduct);
load().catch(e => error(`No se pudo leer la base de datos: ${e.message}`));
