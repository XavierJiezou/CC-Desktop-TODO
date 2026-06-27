// ---- 状态 ----
let todos = [];

const listEl = document.getElementById('list');
const emptyEl = document.getElementById('empty');
const inputEl = document.getElementById('new-todo');
const addBtn = document.getElementById('add-btn');
const opacityEl = document.getElementById('opacity');
const autostartBtn = document.getElementById('autostart');

// 生成简单唯一 id（不依赖外部库）
function uid() {
  return Date.now().toString(36) + Math.random().toString(36).slice(2, 7);
}

function save() {
  window.api.saveTodos(todos);
}

// ---- 渲染 ----
function render() {
  listEl.innerHTML = '';
  emptyEl.classList.toggle('hidden', todos.length > 0);

  for (const todo of todos) {
    const li = document.createElement('li');
    li.className = 'item' + (todo.completed ? ' done' : '');
    li.dataset.id = todo.id;

    const cb = document.createElement('input');
    cb.type = 'checkbox';
    cb.checked = todo.completed;
    cb.addEventListener('change', () => toggle(todo.id));

    const text = document.createElement('span');
    text.className = 'text';
    text.textContent = todo.text;
    // 双击编辑
    text.addEventListener('dblclick', () => startEdit(text, todo.id));

    const del = document.createElement('button');
    del.className = 'del';
    del.textContent = '✕';
    del.title = '删除';
    del.addEventListener('click', () => remove(todo.id));

    li.append(cb, text, del);
    listEl.appendChild(li);
  }
}

// ---- 操作 ----
function add() {
  const value = inputEl.value.trim();
  if (!value) return;
  todos.push({ id: uid(), text: value, completed: false });
  inputEl.value = '';
  save();
  render();
}

function toggle(id) {
  const t = todos.find((x) => x.id === id);
  if (!t) return;
  t.completed = !t.completed;
  save();
  render();
}

function remove(id) {
  todos = todos.filter((x) => x.id !== id);
  save();
  render();
}

function startEdit(textEl, id) {
  textEl.setAttribute('contenteditable', 'true');
  textEl.focus();
  // 选中全部文本
  const range = document.createRange();
  range.selectNodeContents(textEl);
  const sel = window.getSelection();
  sel.removeAllRanges();
  sel.addRange(range);

  const finish = (commit) => {
    textEl.removeAttribute('contenteditable');
    textEl.removeEventListener('blur', onBlur);
    textEl.removeEventListener('keydown', onKey);
    const t = todos.find((x) => x.id === id);
    if (!t) return;
    if (commit) {
      const v = textEl.textContent.trim();
      if (v) t.text = v;
    }
    save();
    render();
  };
  const onBlur = () => finish(true);
  const onKey = (e) => {
    if (e.key === 'Enter') { e.preventDefault(); finish(true); }
    else if (e.key === 'Escape') { finish(false); }
  };
  textEl.addEventListener('blur', onBlur);
  textEl.addEventListener('keydown', onKey);
}

// ---- 透明度 ----
function applyOpacity(value) {
  document.documentElement.style.setProperty('--panel-alpha', value);
}

opacityEl.addEventListener('input', () => {
  const v = parseFloat(opacityEl.value);
  applyOpacity(v);
  window.api.setOpacity(v);
});

// ---- 开机自启 ----
autostartBtn.addEventListener('click', async () => {
  const current = await window.api.getAutostart();
  const next = !current;
  window.api.setAutostart(next);
  autostartBtn.classList.toggle('active', next);
  autostartBtn.title = next ? '开机自启动：开' : '开机自启动：关';
});

// ---- 顶栏按钮 ----
addBtn.addEventListener('click', add);
inputEl.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') add();
});
document.getElementById('close').addEventListener('click', () => window.api.closeWin());
document.getElementById('min').addEventListener('click', () => window.api.minimizeWin());

// ---- 初始化 ----
async function init() {
  const state = await window.api.loadTodos();
  todos = state.todos || [];

  const op = typeof state.opacity === 'number' ? state.opacity : 0.85;
  opacityEl.value = op;
  applyOpacity(op);

  const autostart = await window.api.getAutostart();
  autostartBtn.classList.toggle('active', autostart);
  autostartBtn.title = autostart ? '开机自启动：开' : '开机自启动：关';

  render();
}

init();
