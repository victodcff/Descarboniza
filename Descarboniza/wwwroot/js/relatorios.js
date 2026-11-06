/* Relatórios: filtro por máquina, links de download e versão de impressão (PDF). */
(function () {
    'use strict';

    var raiz = document.getElementById('rel');
    if (!raiz) return;

    var modos = raiz.querySelectorAll('input[name="relModo"]');
    var escolha = document.getElementById('relEscolha');
    var caixas = Array.prototype.slice.call(escolha.querySelectorAll('input[type="checkbox"]'));
    var linhas = Array.prototype.slice.call(raiz.querySelectorAll('tbody tr[data-id]'));
    var csv = document.getElementById('relCsv');
    var botaoPdf = document.getElementById('relImprimir');
    var urlCsv = raiz.dataset.csv;

    function num(v, casas) {
        return v.toLocaleString('pt-BR', { minimumFractionDigits: casas, maximumFractionDigits: casas });
    }
    function texto(id, valor) {
        var e = document.getElementById(id);
        if (e) e.textContent = valor;
    }
    function el(tag, classe, conteudo) {
        var e = document.createElement(tag);
        if (classe) e.className = classe;
        if (conteudo !== undefined) e.textContent = conteudo;
        return e;
    }
    function modoEscolher() {
        return raiz.querySelector('input[name="relModo"]:checked').value === 'escolher';
    }

    var escopoAtual = '';

    /* ---------- Seletor (lista suspensa com busca) ---------- */

    var painel = document.getElementById('relPainel');
    var botaoAbrir = document.getElementById('relAbrir');
    var busca = document.getElementById('relBusca');
    var marcadasEl = document.getElementById('relMarcadas');
    var opcoes = Array.prototype.slice.call(escolha.querySelectorAll('.rel-opcao'));

    function abrirPainel(abrir) {
        painel.hidden = !abrir;
        botaoAbrir.setAttribute('aria-expanded', String(abrir));
        escolha.classList.toggle('aberto', abrir);
        if (abrir) { busca.value = ''; filtrarBusca(); busca.focus(); }
    }

    function filtrarBusca() {
        var termo = busca.value.trim().toLowerCase();
        var algum = false;
        opcoes.forEach(function (o) {
            var bate = !termo || o.dataset.nome.indexOf(termo) !== -1;
            o.hidden = !bate;
            if (bate) algum = true;
        });
        document.getElementById('relSemResultado').hidden = algum;
    }

    function desenharMarcadas() {
        marcadasEl.textContent = '';
        caixas.forEach(function (c) {
            if (!c.checked) return;
            var chip = el('button', 'rel-tag');
            chip.type = 'button';
            chip.title = 'Remover';
            chip.appendChild(el('span', null, c.dataset.nomeOriginal));
            chip.appendChild(el('b', null, '×'));
            chip.addEventListener('click', function () { c.checked = false; atualizar(); });
            marcadasEl.appendChild(chip);
        });
    }

    botaoAbrir.addEventListener('click', function () { abrirPainel(painel.hidden); });
    document.getElementById('relConcluir').addEventListener('click', function () { abrirPainel(false); botaoAbrir.focus(); });
    busca.addEventListener('input', filtrarBusca);

    document.addEventListener('click', function (e) {
        if (!painel.hidden && !escolha.contains(e.target)) abrirPainel(false);
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && !painel.hidden) { abrirPainel(false); botaoAbrir.focus(); }
    });

    function atualizar() {
        var escolher = modoEscolher();
        escolha.hidden = !escolher;
        if (!escolher && !painel.hidden) abrirPainel(false);

        var marcados = caixas.filter(function (c) { return c.checked; }).map(function (c) { return c.value; });
        var usar = escolher ? marcados : null;          // null = todas

        var kwh = 0;
        linhas.forEach(function (tr) {
            var entra = !usar || usar.indexOf(tr.dataset.id) !== -1;
            tr.hidden = !entra;
            if (entra) kwh += parseFloat(tr.dataset.kwh) || 0;
        });

        linhas.forEach(function (tr) {
            if (tr.hidden) return;
            var pct = kwh > 0 ? (parseFloat(tr.dataset.kwh) || 0) / kwh * 100 : 0;
            tr.querySelector('[data-pct]').textContent = num(pct, 1) + '%';
        });

        var qtd = linhas.filter(function (tr) { return !tr.hidden; }).length;
        var custo = 0, co2 = 0;
        linhas.forEach(function (tr) {
            if (tr.hidden) return;
            custo += parseFloat(tr.dataset.custo) || 0;
            co2 += parseFloat(tr.dataset.co2) || 0;
        });

        texto('relTfKwh', num(kwh, 2));
        texto('relTfCusto', num(custo, 2));
        texto('relTfCo2', num(co2, 2));

        escopoAtual = !escolher
            ? 'Todas as máquinas · ' + linhas.length + (linhas.length === 1 ? ' equipamento' : ' equipamentos')
            : (marcados.length === 0
                ? 'Nenhuma máquina selecionada'
                : marcados.length + (marcados.length === 1 ? ' máquina selecionada' : ' máquinas selecionadas') +
                  ' · ' + num(kwh, 2) + ' kWh no mês');

        texto('relEscopo', escopoAtual);
        texto('relDownloadEscopo', escopoAtual);
        texto('relContagem', escolher && marcados.length ? '(' + marcados.length + ')' : '');
        texto('relAbrirTexto', marcados.length === 0
            ? 'Escolher máquinas'
            : marcados.length + (marcados.length === 1 ? ' máquina' : ' máquinas') + ' · editar');
        desenharMarcadas();

        // Sem nada marcado não há o que baixar
        var vazio = escolher && marcados.length === 0;
        csv.classList.toggle('desativado', vazio);
        csv.setAttribute('aria-disabled', String(vazio));
        botaoPdf.disabled = vazio;
        csv.href = usar ? urlCsv + '?ids=' + encodeURIComponent(marcados.join(',')) : urlCsv;
    }

    /* ---------- Versão de impressão ---------- */

    function montarImpressao() {
        var visiveis = linhas.filter(function (tr) { return !tr.hidden; });
        var kwh = 0, custo = 0, co2 = 0;
        visiveis.forEach(function (tr) {
            kwh += parseFloat(tr.dataset.kwh) || 0;
            custo += parseFloat(tr.dataset.custo) || 0;
            co2 += parseFloat(tr.dataset.co2) || 0;
        });

        texto('riEscopo', escopoAtual);
        texto('riKwh', num(kwh, 2) + ' kWh');
        texto('riCusto', 'R$ ' + num(custo, 2));
        texto('riCo2', num(co2, 2) + ' kg CO₂');
        texto('riQtd', String(visiveis.length));

        var corpo = document.getElementById('riCorpo');
        corpo.textContent = '';

        visiveis.forEach(function (tr) {
            var c = tr.querySelectorAll('td');
            var pct = kwh > 0 ? (parseFloat(tr.dataset.kwh) || 0) / kwh * 100 : 0;

            var linha = el('tr');
            linha.appendChild(el('td', 'ri-nome', c[0].textContent));
            [1, 2, 3, 4, 5].forEach(function (i) { linha.appendChild(el('td', 'ri-num', c[i].textContent)); });

            var celula = el('td', 'ri-part');
            var barra = el('span', 'ri-barra');
            var preench = el('i');
            preench.style.width = Math.max(2, Math.min(100, pct)) + '%';
            barra.appendChild(preench);
            celula.appendChild(barra);
            celula.appendChild(el('b', null, num(pct, 1) + '%'));
            linha.appendChild(celula);

            corpo.appendChild(linha);
        });

        var rodape = document.getElementById('riRodape');
        rodape.textContent = '';
        var tot = el('tr');
        tot.appendChild(el('td', 'ri-nome', 'Total'));
        tot.appendChild(el('td'));
        tot.appendChild(el('td'));
        tot.appendChild(el('td', 'ri-num', num(kwh, 2)));
        tot.appendChild(el('td', 'ri-num', 'R$ ' + num(custo, 2)));
        tot.appendChild(el('td', 'ri-num', num(co2, 2)));
        tot.appendChild(el('td', 'ri-num', '100%'));
        rodape.appendChild(tot);

        // Frase de destaque
        var destaque = document.getElementById('riDestaque');
        if (visiveis.length) {
            var maior = visiveis[0];
            visiveis.forEach(function (tr) {
                if ((parseFloat(tr.dataset.kwh) || 0) > (parseFloat(maior.dataset.kwh) || 0)) maior = tr;
            });
            var pctMaior = kwh > 0 ? (parseFloat(maior.dataset.kwh) || 0) / kwh * 100 : 0;
            destaque.textContent = visiveis.length > 1
                ? 'Maior consumidor: ' + maior.querySelector('td').textContent + ', com ' + num(pctMaior, 1) +
                  '% do consumo do período (' + num(parseFloat(maior.dataset.kwh) || 0, 2) + ' kWh/mês).'
                : '';
            destaque.hidden = visiveis.length <= 1;
        }
    }

    window.addEventListener('beforeprint', montarImpressao);

    /* ---------- Eventos ---------- */

    modos.forEach(function (m) {
        m.addEventListener('change', function () {
            atualizar();
            if (modoEscolher() && !caixas.some(function (c) { return c.checked; })) abrirPainel(true);
        });
    });
    caixas.forEach(function (c) { c.addEventListener('change', atualizar); });

    document.getElementById('relTodas').addEventListener('click', function () {
        caixas.forEach(function (c) { c.checked = true; });
        atualizar();
    });
    document.getElementById('relNenhuma').addEventListener('click', function () {
        caixas.forEach(function (c) { c.checked = false; });
        atualizar();
    });

    csv.addEventListener('click', function (e) {
        if (csv.classList.contains('desativado')) { e.preventDefault(); return; }
        // É um download (a página não troca): impede a animação de saída do site,
        // que deixaria o conteúdo invisível depois de baixar.
        e.stopPropagation();
    });

    // Segurança: se a página voltar do histórico com a animação de saída ativa, desfaz.
    window.addEventListener('pageshow', function () {
        var area = document.querySelector('.content-area');
        if (area) area.classList.remove('page-exit');
    });

    botaoPdf.addEventListener('click', function () {
        montarImpressao();
        window.print();
    });

    atualizar();
})();
