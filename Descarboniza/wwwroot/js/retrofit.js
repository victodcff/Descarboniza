/* Sugestões de retrofit (tela Consumo/Analise).
   Todo texto vindo da IA entra na página com textContent (nunca innerHTML). */
(function () {
    'use strict';

    var card = document.getElementById('retrofit');
    if (!card) return;

    var botao = document.getElementById('rt-gerar');
    var status = document.getElementById('rt-status');
    var resultado = document.getElementById('rt-resultado');
    var temporizador = null;

    var NIVEIS = { baixo: 1, medio: 2, alto: 3 };

    function el(tag, classe, texto) {
        var e = document.createElement(tag);
        if (classe) e.className = classe;
        if (texto !== undefined && texto !== null) e.textContent = texto;
        return e;
    }

    function preenchido(texto) {
        return typeof texto === 'string' && texto.trim().length > 0;
    }

    function num(valor, casas) {
        return Number(valor).toLocaleString('pt-BR', {
            minimumFractionDigits: casas,
            maximumFractionDigits: casas
        });
    }

    function rotuloNivel(valor) {
        return valor === 'medio' ? 'Médio' : valor === 'alto' ? 'Alto' : 'Baixo';
    }

    function mostrarStatus(texto, erro) {
        status.hidden = !texto;
        status.textContent = texto || '';
        status.classList.toggle('rt-status--erro', !!erro);
    }

    var NS = 'http://www.w3.org/2000/svg';

    function icone(caminhos) {
        var svg = document.createElementNS(NS, 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '1.9');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');
        svg.setAttribute('aria-hidden', 'true');
        caminhos.forEach(function (d) {
            var p = document.createElementNS(NS, 'path');
            p.setAttribute('d', d);
            svg.appendChild(p);
        });
        return svg;
    }

    var ICONES = {
        energia: ['M13 2 4 14h7l-1 8 9-12h-7z'],
        co2: ['M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.5 19 2c1 2 2 4.5 2 8 0 5.5-4.8 10-10 10Z', 'M2 21c0-3 1.9-5.5 6-7'],
        dinheiro: ['M12 2v20', 'M17 6H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6']
    };

    /* Semáforo: a cor mostra se é vantajoso.
       Impacto alto = verde (bom). Esforço alto = vermelho (exige mais). */
    function pilula(rotulo, valor, quantoMaiorMelhor) {
        var n = NIVEIS[valor] || 1;
        var qualidade = quantoMaiorMelhor ? n : 4 - n;      // 3 = bom, 2 = médio, 1 = ruim
        var cor = qualidade >= 3 ? 'bom' : (qualidade === 2 ? 'medio' : 'ruim');
        var p = el('span', 'rt-pil rt-pil--' + cor);
        p.title = rotulo + ' ' + rotuloNivel(valor).toLowerCase();
        p.appendChild(el('span', 'rt-pil__rot', rotulo));
        var pontos = el('span', 'rt-pil__pontos');
        pontos.setAttribute('aria-hidden', 'true');
        for (var i = 1; i <= 3; i++) pontos.appendChild(el('i', i <= n ? 'on' : ''));
        p.appendChild(pontos);
        p.appendChild(el('b', null, rotuloNivel(valor)));
        return p;
    }

    function nota(rotulo, texto, tipo) {
        var d = el('div', 'rt-nota rt-nota--' + tipo);
        d.appendChild(el('span', null, rotulo));
        d.appendChild(el('p', null, texto));
        return d;
    }

    function estatistica(chave, rotulo) {
        var e = el('div', 'rt-stat');
        var ic = el('span', 'rt-stat__icone');
        ic.appendChild(icone(ICONES[chave]));
        e.appendChild(ic);
        var t = el('span', 'rt-stat__texto');
        var v = el('strong');
        t.appendChild(v);
        t.appendChild(el('small', null, rotulo));
        e.appendChild(t);
        return { caixa: e, valor: v };
    }

    /* Painel liso (sem gradiente): economia por ano + seletor 10/20/30 %. */
    function desenharDestaque(cenarios) {
        var bloco = el('div', 'rt-hero');

        var topo = el('div', 'rt-hero__topo');
        var ic = el('span', 'rt-hero__icone');
        ic.appendChild(icone(ICONES.dinheiro));
        topo.appendChild(ic);
        var tt = el('div');
        tt.appendChild(el('span', 'rt-hero__rot', 'Economia potencial por ano'));
        var valor = el('strong', 'rt-hero__valor');
        tt.appendChild(valor);
        topo.appendChild(tt);
        bloco.appendChild(topo);

        var stats = el('div', 'rt-stats');
        var sEnergia = estatistica('energia', 'de energia a menos por mês');
        var sCo2 = estatistica('co2', 'de CO₂ a menos por ano');
        stats.appendChild(sEnergia.caixa);
        stats.appendChild(sCo2.caixa);
        bloco.appendChild(stats);

        var seletor = el('div', 'rt-seg');
        seletor.setAttribute('role', 'group');
        seletor.setAttribute('aria-label', 'Quanto o consumo cairia');
        seletor.appendChild(el('span', 'rt-seg__rot', 'Se o consumo cair'));
        var botoes = [];

        function mostrar(i) {
            var c = cenarios[i];
            valor.textContent = 'R$ ' + num(c.economiaReaisAno, 2);
            sEnergia.valor.textContent = num(c.economiaKwhMes, 0) + ' kWh';
            sCo2.valor.textContent = num(c.economiaCo2KgAno, 0) + ' kg';
            botoes.forEach(function (b, k) {
                b.classList.toggle('ativo', k === i);
                b.setAttribute('aria-pressed', String(k === i));
            });
        }

        cenarios.forEach(function (c, i) {
            var b = el('button', 'rt-seg__op', c.reducaoPercentual + '%');
            b.type = 'button';
            b.addEventListener('click', function () { mostrar(i); });
            botoes.push(b);
            seletor.appendChild(b);
        });
        bloco.appendChild(seletor);

        mostrar(Math.min(1, cenarios.length - 1));
        return bloco;
    }

    var VISIVEIS = 3;

    function desenhar(dados) {
        resultado.textContent = '';

        if (dados.cenarios && dados.cenarios.length) {
            resultado.appendChild(desenharDestaque(dados.cenarios));
        }

        var sugestoes = dados.sugestoes || [];

        if (sugestoes.length) {
            resultado.appendChild(el('h3', 'rt-titulo', 'Melhorias sugeridas'));

            var legenda = el('p', 'rt-legenda');
            [['bom', 'Vantajoso'], ['medio', 'Moderado'], ['ruim', 'Exige mais']].forEach(function (x) {
                var item = el('span');
                item.appendChild(el('i', 'rt-leg rt-leg--' + x[0]));
                item.appendChild(document.createTextNode(x[1]));
                legenda.appendChild(item);
            });
            resultado.appendChild(legenda);

            var lista = el('ol', 'rt-lista');

            sugestoes.forEach(function (s, i) {
                var item = el('li', 'rt-item');
                if (i >= VISIVEIS) item.hidden = true;

                var topoItem = el('button', 'rt-item__topo');
                topoItem.type = 'button';
                topoItem.setAttribute('aria-expanded', 'false');

                topoItem.appendChild(el('span', 'rt-numero', String(i + 1)));

                var texto = el('span', 'rt-item__texto');
                texto.appendChild(el('strong', null, s.titulo));
                var pilulas = el('span', 'rt-item__pilulas');
                pilulas.appendChild(pilula('Impacto', s.impacto, true));
                pilulas.appendChild(pilula('Esforço', s.esforco, false));
                texto.appendChild(pilulas);
                topoItem.appendChild(texto);

                var seta = el('span', 'rt-seta');
                seta.setAttribute('aria-hidden', 'true');
                topoItem.appendChild(seta);
                item.appendChild(topoItem);

                var corpo = el('div', 'rt-corpo');
                corpo.hidden = true;
                if (preenchido(s.descricao)) corpo.appendChild(el('p', 'rt-desc', s.descricao));
                if (preenchido(s.motivo)) corpo.appendChild(nota('Por que', s.motivo, 'ok'));
                if (preenchido(s.verificarAntes)) corpo.appendChild(nota('Antes de investir', s.verificarAntes, 'alerta'));
                item.appendChild(corpo);

                topoItem.addEventListener('click', function () {
                    var abrir = corpo.hidden;
                    corpo.hidden = !abrir;
                    topoItem.setAttribute('aria-expanded', String(abrir));
                    item.classList.toggle('aberto', abrir);
                });

                lista.appendChild(item);
            });
            resultado.appendChild(lista);

            if (sugestoes.length > VISIVEIS) {
                var mais = el('button', 'rt-mais', 'Ver mais ' + (sugestoes.length - VISIVEIS));
                mais.type = 'button';
                mais.addEventListener('click', function () {
                    lista.querySelectorAll('.rt-item[hidden]').forEach(function (li) { li.hidden = false; });
                    mais.remove();
                });
                resultado.appendChild(mais);
            }
        }

        var rodape = el('p', 'rt-rodape',
            'Sugestões geradas automaticamente. Os valores da simulação são calculados pelo sistema com a sua tarifa e não são uma promessa de economia.');
        (dados.avisos || []).forEach(function (a) {
            if (preenchido(a)) rodape.appendChild(document.createTextNode(' ' + a));
        });
        resultado.appendChild(rodape);

        resultado.hidden = false;
    }

    function terminar() {
        clearTimeout(temporizador);
        botao.disabled = false;
        botao.textContent = 'Atualizar';
    }

    botao.addEventListener('click', function () {
        botao.disabled = true;
        botao.textContent = 'Carregando...';
        resultado.hidden = true;
        mostrarStatus('Analisando a máquina...');

        // O serviço de IA hospedado pode levar cerca de 1 minuto para iniciar.
        temporizador = setTimeout(function () {
            mostrarStatus('O serviço de análise estava desligado e está sendo iniciado. Isso pode levar até 1 minuto, aguarde um instante.');
        }, 8000);

        var form = new FormData();
        form.append('id', card.dataset.id);
        form.append('__RequestVerificationToken',
            card.querySelector('input[name="__RequestVerificationToken"]').value);

        fetch(card.dataset.url, {
            method: 'POST',
            body: form,
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        })
            .then(function (res) {
                return res.json().catch(function () { return null; });
            })
            .then(function (dados) {
                terminar();

                if (!dados) {
                    mostrarStatus('Não foi possível gerar agora. Atualize a página e tente de novo.', true);
                    return;
                }
                if (!dados.sucesso) {
                    mostrarStatus(dados.erro || 'Não foi possível gerar as sugestões agora.', true);
                    return;
                }

                mostrarStatus('');
                desenhar(dados);
            })
            .catch(function () {
                terminar();
                mostrarStatus('Sem conexão com o servidor. Tente novamente.', true);
            });
    });
})();
