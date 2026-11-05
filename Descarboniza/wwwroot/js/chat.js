/* Assistente com IA (botão flutuante): arrastar, fixar, minimizar e fechar.
   A conversa fica só na aba do navegador (sessionStorage) e some ao fechá-la.
   Todo texto vindo da IA entra na página com textContent (nunca innerHTML). */
(function () {
    'use strict';

    var widget = document.getElementById('chat-widget');
    if (!widget) return;

    var painel = document.getElementById('chat-painel');
    var abrir = document.getElementById('chat-abrir');
    var fechar = document.getElementById('chat-fechar');
    var limpar = document.getElementById('chat-limpar');
    var msgs = document.getElementById('chat-msgs');
    var sugestoes = document.getElementById('chat-sugestoes');
    var form = document.getElementById('chat-form');
    var campo = document.getElementById('chat-texto');
    var enviar = document.getElementById('chat-enviar');
    var topo = document.getElementById('chat-topo');
    var fixar = document.getElementById('chat-fixar');
    var minimizar = document.getElementById('chat-minimizar');
    var AVATAR = widget.dataset.avatar || '';
    var GITHUB = widget.dataset.github || '';

    var BOAS_VINDAS = 'Oi! Eu sou o Eco, assistente do Descarboniza. Posso falar sobre o consumo, o custo e o CO₂ das suas máquinas, e explicar como reduzir o gasto de energia. O que você quer saber?';
    var MAX_HISTORICO = 12;

    var historico = [];     // [{ papel: 'usuario' | 'ia', texto: '...' }]
    var aguardando = false;
    var temporizador = null;

    function el(tag, classe, texto) {
        var e = document.createElement(tag);
        if (classe) e.className = classe;
        if (texto !== undefined && texto !== null) e.textContent = texto;
        return e;
    }

    /* **negrito** vira <strong>; o resto entra como texto puro. */
    function comNegrito(pai, texto) {
        texto.split(/(\*\*[^*]+\*\*)/g).forEach(function (parte) {
            if (!parte) return;
            if (parte.slice(0, 2) === '**' && parte.slice(-2) === '**' && parte.length > 4) {
                pai.appendChild(el('strong', null, parte.slice(2, -2)));
            } else {
                pai.appendChild(document.createTextNode(parte));
            }
        });
    }

    /* Parágrafos, listas com "- " e quebras de linha, sem usar innerHTML. */
    function renderizar(pai, texto) {
        var lista = null;

        texto.split('\n').forEach(function (linha, i, todas) {
            var item = linha.match(/^\s*[-*•]\s+(.*)$/);

            if (item) {
                if (!lista) { lista = el('ul', 'chat-lista'); pai.appendChild(lista); }
                var li = el('li');
                comNegrito(li, item[1]);
                lista.appendChild(li);
                return;
            }

            lista = null;
            if (!linha.trim()) return;

            var p = el('p');
            comNegrito(p, linha.trim());
            pai.appendChild(p);
        });
    }

    function rolar() {
        msgs.scrollTop = msgs.scrollHeight;
    }

    function avatarMini() {
        var img = el('img', 'chat-msg__avatar');
        img.src = AVATAR;
        img.alt = '';
        img.width = 28;
        img.height = 28;
        img.draggable = false;
        return img;
    }

    function adicionar(papel, texto, tipo) {
        var linha = el('div', 'chat-msg chat-msg--' + papel + (tipo ? ' chat-msg--' + tipo : ''));
        var balao = el('div', 'chat-balao');

        if (papel === 'ia') renderizar(balao, texto);
        else balao.textContent = texto;

        if (papel === 'ia' && AVATAR) linha.appendChild(avatarMini());
        linha.appendChild(balao);
        msgs.appendChild(linha);
        rolar();
        return linha;
    }

    function mostrarDigitando() {
        var linha = el('div', 'chat-msg chat-msg--ia chat-msg--digitando');
        var balao = el('div', 'chat-balao');
        balao.setAttribute('aria-label', 'O assistente está escrevendo');

        var pontos = el('span', 'chat-pontos');
        for (var i = 0; i < 3; i++) pontos.appendChild(el('i'));
        balao.appendChild(pontos);

        if (AVATAR) linha.appendChild(avatarMini());
        linha.appendChild(balao);
        msgs.appendChild(linha);
        rolar();

        // O serviço de IA hospedado pode levar cerca de 1 minuto para iniciar.
        temporizador = setTimeout(function () {
            balao.textContent = 'O assistente estava desligado e está sendo iniciado. Isso pode levar até 1 minuto, aguarde um instante.';
            balao.classList.add('chat-balao--nota');
            rolar();
        }, 8000);

        return linha;
    }

    function ajustarAltura() {
        campo.style.height = 'auto';
        campo.style.height = Math.min(campo.scrollHeight + 2, 110) + 'px';
    }

    function definirAguardando(valor) {
        aguardando = valor;
        enviar.disabled = valor;
        campo.disabled = valor;
        if (!valor) campo.focus();
    }

    function reiniciar() {
        historico = [];
        msgs.textContent = '';
        adicionar('ia', BOAS_VINDAS);
        sugestoes.hidden = false;
        salvarHistorico();
    }

    /* ---------- Estado da janela (posição, fixada, minimizada) ---------- */

    var estado = { x: null, y: null, fixado: false, min: false };

    function lerArmazenado(tipo, chave) {
        try { return JSON.parse(window[tipo].getItem(chave)); } catch (e) { return null; }
    }
    function gravarArmazenado(tipo, chave, valor) {
        try { window[tipo].setItem(chave, JSON.stringify(valor)); } catch (e) { /* sem armazenamento: tudo bem */ }
    }

    function salvarEstado() { gravarArmazenado('localStorage', 'dsc-chat-janela', estado); }
    function salvarHistorico() { gravarArmazenado('sessionStorage', 'dsc-chat-historico', historico); }

    function telaPequena() { return window.matchMedia('(max-width: 640px)').matches; }

    /* Mantém a janela inteira dentro da tela. */
    function limitar(x, y) {
        var larg = painel.offsetWidth, alt = painel.offsetHeight;
        return {
            x: Math.min(Math.max(8, x), Math.max(8, window.innerWidth - larg - 8)),
            y: Math.min(Math.max(8, y), Math.max(8, window.innerHeight - alt - 8))
        };
    }

    function aplicarPosicao() {
        if (telaPequena() || estado.x === null) {
            painel.style.left = painel.style.top = painel.style.right = painel.style.bottom = '';
            painel.classList.remove('chat-livre');
            return;
        }
        var pos = limitar(estado.x, estado.y);
        painel.classList.add('chat-livre');
        painel.style.left = pos.x + 'px';
        painel.style.top = pos.y + 'px';
        painel.style.right = 'auto';
        painel.style.bottom = 'auto';
    }

    function aplicarEstado() {
        painel.classList.toggle('chat-min', estado.min);
        painel.classList.toggle('chat-fixado', estado.fixado);

        fixar.setAttribute('aria-pressed', String(estado.fixado));
        fixar.title = estado.fixado ? 'Soltar posição' : 'Fixar posição';
        fixar.setAttribute('aria-label', fixar.title);

        minimizar.setAttribute('aria-pressed', String(estado.min));
        minimizar.title = estado.min ? 'Restaurar' : 'Minimizar';
        minimizar.setAttribute('aria-label', minimizar.title);

        topo.title = estado.fixado ? '' : 'Arraste para mover';
        aplicarPosicao();
    }

    function abrirPainel() {
        painel.hidden = false;
        abrir.setAttribute('aria-expanded', 'true');
        widget.classList.add('chat-aberto');
        gravarArmazenado('sessionStorage', 'dsc-chat-aberto', true);
        aplicarEstado();
        rolar();
        if (!estado.min) campo.focus();
    }

    function fecharPainel() {
        painel.hidden = true;
        abrir.setAttribute('aria-expanded', 'false');
        widget.classList.remove('chat-aberto');
        gravarArmazenado('sessionStorage', 'dsc-chat-aberto', false);
        abrir.focus();
    }

    function alternarMinimizar() {
        estado.min = !estado.min;
        salvarEstado();
        aplicarEstado();
        if (!estado.min) { rolar(); campo.focus(); }
    }

    function alternarFixar() {
        estado.fixado = !estado.fixado;
        salvarEstado();
        aplicarEstado();
    }

    /* ---------- Arrastar pelo cabeçalho ---------- */

    var arrasto = null;

    topo.addEventListener('pointerdown', function (e) {
        if (e.button !== 0 || e.target.closest('button') || estado.fixado || telaPequena()) return;

        var r = painel.getBoundingClientRect();
        arrasto = { dx: e.clientX - r.left, dy: e.clientY - r.top };
        topo.setPointerCapture(e.pointerId);
        painel.classList.add('chat-arrastando');
        e.preventDefault();
    });

    topo.addEventListener('pointermove', function (e) {
        if (!arrasto) return;
        var pos = limitar(e.clientX - arrasto.dx, e.clientY - arrasto.dy);
        estado.x = pos.x;
        estado.y = pos.y;
        aplicarPosicao();
    });

    function soltar(e) {
        if (!arrasto) return;
        arrasto = null;
        painel.classList.remove('chat-arrastando');
        try { topo.releasePointerCapture(e.pointerId); } catch (x) { /* já solto */ }
        salvarEstado();
    }
    topo.addEventListener('pointerup', soltar);
    topo.addEventListener('pointercancel', soltar);

    // Duplo clique no cabeçalho minimiza/restaura
    topo.addEventListener('dblclick', function (e) {
        if (!e.target.closest('button')) alternarMinimizar();
    });

    window.addEventListener('resize', function () { if (!painel.hidden) aplicarPosicao(); });

    function consumoAberto() {
        var r = document.getElementById('retrofit');
        var id = r ? parseInt(r.dataset.id, 10) : NaN;
        return isNaN(id) ? null : id;
    }

    /* ---------- Perguntas sobre quem criou o sistema (resposta local, sem chamar a IA) ---------- */

    function semAcento(t) {
        return t.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    }

    var PADROES_CRIADORES = [
        /\b(criador|criadores|desenvolvedor|desenvolvedores|programador|programadores|autor|autores)\b/,
        /\bquem\b.*\b(criou|criaram|fez|fizeram|desenvolveu|desenvolveram|programou|programaram|construiu|construiram|projetou|idealizou)\b/,
        /\b(feito|criado|desenvolvido|programado|construido)\s+por\b/,
        /\b(github|repositorio|codigo[- ]fonte)\b/,
        /\bequipe\s+(de|do|da)\s+(desenvolvimento|projeto|sistema|tcc)\b/
    ];

    function perguntaSobreCriadores(texto) {
        var t = semAcento(texto);
        return PADROES_CRIADORES.some(function (r) { return r.test(t); });
    }

    function cartaoCriadores() {
        var linha = el('div', 'chat-msg chat-msg--ia');
        if (AVATAR) linha.appendChild(avatarMini());

        var balao = el('div', 'chat-balao');
        balao.appendChild(el('p', null, 'O Descarboniza foi desenvolvido por uma equipe de estudantes do SENAI, como TCC do curso técnico de Desenvolvimento de Sistemas.'));
        balao.appendChild(el('p', null, 'Eu sou o Eco, o assistente virtual (IA) do sistema. O projeto e o código estão no GitHub:'));

        if (GITHUB) {
            var link = el('a', 'chat-cartao');
            link.href = GITHUB;
            link.target = '_blank';
            link.rel = 'noopener noreferrer';

            var icone = el('span', 'chat-cartao__icone');
            icone.innerHTML = '<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M12 .5A11.5 11.5 0 0 0 .5 12c0 5.08 3.29 9.39 7.86 10.91.58.1.79-.25.79-.56v-2c-3.2.7-3.88-1.37-3.88-1.37-.52-1.33-1.28-1.69-1.28-1.69-1.04-.71.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.03 1.76 2.7 1.25 3.36.96.1-.75.4-1.25.73-1.54-2.56-.29-5.25-1.28-5.25-5.69 0-1.26.45-2.29 1.19-3.1-.12-.29-.52-1.46.11-3.05 0 0 .97-.31 3.18 1.18a11 11 0 0 1 5.8 0c2.2-1.49 3.17-1.18 3.17-1.18.63 1.59.23 2.76.11 3.05.74.81 1.19 1.84 1.19 3.1 0 4.42-2.7 5.4-5.27 5.68.41.36.78 1.06.78 2.14v3.17c0 .31.21.67.8.56A11.5 11.5 0 0 0 23.5 12 11.5 11.5 0 0 0 12 .5Z"/></svg>';
            link.appendChild(icone);

            var texto = el('span', 'chat-cartao__texto');
            texto.appendChild(el('strong', null, 'Descarboniza no GitHub'));
            texto.appendChild(el('small', null, GITHUB.replace(/^https?:\/\//, '')));
            link.appendChild(texto);

            var seta = el('span', 'chat-cartao__seta');
            seta.textContent = '↗';
            seta.setAttribute('aria-hidden', 'true');
            link.appendChild(seta);

            balao.appendChild(link);
        }

        linha.appendChild(balao);
        msgs.appendChild(linha);
        rolar();
    }

    function responderCriadores() {
        definirAguardando(true);
        var digitando = mostrarDigitando();
        // Sem chamar o servidor: some o aviso de "iniciando" logo
        clearTimeout(temporizador);

        setTimeout(function () {
            digitando.remove();
            cartaoCriadores();
            historico.push({ papel: 'ia', texto: 'O Descarboniza foi desenvolvido por uma equipe de estudantes do SENAI, como TCC. O projeto está no GitHub.' });
            salvarHistorico();
            definirAguardando(false);
        }, 700);
    }

    function enviarMensagem(texto) {
        texto = (texto || '').trim();
        if (!texto || aguardando) return;

        sugestoes.hidden = true;
        adicionar('usuario', texto);
        historico.push({ papel: 'usuario', texto: texto });
        salvarHistorico();
        campo.value = '';
        ajustarAltura();

        if (perguntaSobreCriadores(texto)) {
            responderCriadores();
            return;
        }

        definirAguardando(true);
        var digitando = mostrarDigitando();

        var token = widget.querySelector('input[name="__RequestVerificationToken"]');

        fetch(widget.dataset.url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'RequestVerificationToken': token ? token.value : ''
            },
            body: JSON.stringify({
                mensagens: historico.slice(-MAX_HISTORICO),
                idConsumo: consumoAberto()
            })
        })
            .then(function (res) {
                return res.json().catch(function () { return null; });
            })
            .then(function (dados) {
                clearTimeout(temporizador);
                digitando.remove();

                if (dados && dados.sucesso && dados.resposta) {
                    historico.push({ papel: 'ia', texto: dados.resposta });
                    adicionar('ia', dados.resposta);
                    salvarHistorico();
                } else {
                    falhar(texto, (dados && dados.erro) || 'Não foi possível responder agora. Tente novamente.');
                }
            })
            .catch(function () {
                clearTimeout(temporizador);
                digitando.remove();
                falhar(texto, 'Sem conexão com o servidor. Tente novamente.');
            })
            .then(function () {
                definirAguardando(false);
            });
    }

    /* Em caso de erro, a pergunta não entra no histórico e volta para o campo,
       então basta apertar Enter de novo. */
    function falhar(texto, mensagem) {
        historico.pop();
        salvarHistorico();
        adicionar('ia', mensagem, 'erro');
        campo.value = texto;
        ajustarAltura();
    }

    abrir.addEventListener('click', function () {
        if (painel.hidden) abrirPainel(); else fecharPainel();
    });
    fechar.addEventListener('click', fecharPainel);
    fixar.addEventListener('click', alternarFixar);
    minimizar.addEventListener('click', alternarMinimizar);
    limpar.addEventListener('click', function () { if (!aguardando) reiniciar(); });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !painel.hidden && !estado.fixado) fecharPainel();
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        enviarMensagem(campo.value);
    });

    campo.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            enviarMensagem(campo.value);
        }
    });
    campo.addEventListener('input', ajustarAltura);

    sugestoes.addEventListener('click', function (e) {
        var chip = e.target.closest('.chat-chip');
        if (chip) enviarMensagem(chip.textContent);
    });

    /* Qualquer elemento com data-chat-abrir (ex.: item "Assistente de IA" do menu) abre/fecha o chat. */
    document.addEventListener('click', function (e) {
        var gatilho = e.target.closest('[data-chat-abrir]');
        if (!gatilho) return;

        e.preventDefault();

        // No celular o menu é uma gaveta: fecha antes de abrir o chat.
        var gaveta = document.getElementById('sidebarClose');
        if (gaveta && gaveta.offsetParent !== null) gaveta.click();

        if (painel.hidden) abrirPainel(); else fecharPainel();
    });

    // Restaura estado e conversa desta aba
    var guardado = lerArmazenado('localStorage', 'dsc-chat-janela');
    if (guardado && typeof guardado === 'object') {
        estado.x = typeof guardado.x === 'number' ? guardado.x : null;
        estado.y = typeof guardado.y === 'number' ? guardado.y : null;
        estado.fixado = guardado.fixado === true;
        estado.min = guardado.min === true;
    }

    var salvo = lerArmazenado('sessionStorage', 'dsc-chat-historico');
    if (Array.isArray(salvo) && salvo.length) {
        historico = salvo
            .filter(function (m) { return m && (m.papel === 'usuario' || m.papel === 'ia') && typeof m.texto === 'string'; })
            .slice(-MAX_HISTORICO);
        msgs.textContent = '';
        adicionar('ia', BOAS_VINDAS);
        historico.forEach(function (m) { adicionar(m.papel, m.texto); });
        sugestoes.hidden = true;
    } else {
        reiniciar();
    }

    // Janela fixada continua aberta ao trocar de página
    if (estado.fixado && lerArmazenado('sessionStorage', 'dsc-chat-aberto') === true) {
        abrirPainel();
    } else {
        aplicarEstado();
    }
})();
