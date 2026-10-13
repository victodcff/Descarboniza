(function () {

    /* =========================================================
       TEMA
       ========================================================= */

    const temaSalvo = localStorage.getItem("descarboniza-tema");

    if (temaSalvo === "dark") {
        document.documentElement.setAttribute("data-theme", "dark");
    }


    function atualizarBotaoTema() {

        const botao = document.getElementById("themeToggle");

        if (!botao) return;

        const temaEscuro =
            document.documentElement.getAttribute("data-theme") === "dark";

        const iconeSol =
            botao.querySelector(".theme-icon-sun");

        const iconeLua =
            botao.querySelector(".theme-icon-moon");

        if (iconeSol && iconeLua) {

            iconeSol.style.display =
                temaEscuro ? "none" : "block";

            iconeLua.style.display =
                temaEscuro ? "block" : "none";
        }

        botao.setAttribute(
            "aria-label",
            temaEscuro
                ? "Ativar modo claro"
                : "Ativar modo escuro"
        );

        botao.setAttribute(
            "title",
            temaEscuro
                ? "Modo claro"
                : "Modo escuro"
        );
    }


    document.addEventListener(
        "DOMContentLoaded",
        function () {

            const botao =
                document.getElementById("themeToggle");

            if (!botao) return;

            atualizarBotaoTema();

            botao.addEventListener(
                "click",
                function () {

                    const temaEscuro =
                        document.documentElement
                            .getAttribute("data-theme") === "dark";

                    if (temaEscuro) {

                        document.documentElement
                            .removeAttribute("data-theme");

                        localStorage.setItem(
                            "descarboniza-tema",
                            "light"
                        );

                    } else {

                        document.documentElement
                            .setAttribute(
                                "data-theme",
                                "dark"
                            );

                        localStorage.setItem(
                            "descarboniza-tema",
                            "dark"
                        );
                    }

                    atualizarBotaoTema();
                }
            );
        }
    );


    /* =========================================================
       TRANSIÇÃO ENTRE PÁGINAS
       ========================================================= */

    document.addEventListener(
        "click",
        function (event) {

            const link =
                event.target.closest("a");

            if (!link) return;


            const content =
                document.querySelector(".content-area");

            if (!content) return;


            const href =
                link.getAttribute("href");

            if (!href) return;


            /* -------------------------------------------------
               IGNORA LINKS ESPECIAIS
               ------------------------------------------------- */

            if (
                href.startsWith("#") ||
                href.startsWith("javascript:") ||
                href.startsWith("mailto:") ||
                href.startsWith("tel:")
            ) {
                return;
            }


            /* -------------------------------------------------
               IGNORA NOVA ABA
               ------------------------------------------------- */

            if (
                event.ctrlKey ||
                event.shiftKey ||
                event.altKey ||
                event.metaKey ||
                link.target === "_blank"
            ) {
                return;
            }


            /* -------------------------------------------------
               VERIFICA URL
               ------------------------------------------------- */

            const url =
                new URL(
                    link.href,
                    window.location.href
                );


            /* -------------------------------------------------
               IGNORA LINKS EXTERNOS
               ------------------------------------------------- */

            if (
                url.origin !==
                window.location.origin
            ) {
                return;
            }


            /* -------------------------------------------------
               IGNORA PÁGINA ATUAL
               ------------------------------------------------- */

            if (
                url.pathname ===
                window.location.pathname &&
                url.search ===
                window.location.search
            ) {
                return;
            }


            /* -------------------------------------------------
               COMEÇA A TRANSIÇÃO
               ------------------------------------------------- */

            event.preventDefault();

            content.classList.add("page-exit");


            /* -------------------------------------------------
               TROCA DE PÁGINA
               ------------------------------------------------- */

            setTimeout(
                function () {

                    window.location.href =
                        link.href;

                },
                160
            );

        }
    );
    /* =========================================================
   MENU MOBILE (HAMBÚRGUER)
   ========================================================= */

    document.addEventListener(
        "DOMContentLoaded",
        function () {

            const toggle = document.getElementById("menuToggle");
            const sidebar = document.getElementById("appSidebar");
            const backdrop = document.getElementById("sidebarBackdrop");
            const fechar = document.getElementById("sidebarClose");

            if (!toggle || !sidebar) return;

            function menuAberto() {
                return document.body.classList.contains("menu-open");
            }

            function definirMenu(aberto) {

                const focoNoMenu = sidebar.contains(document.activeElement);

                document.body.classList.toggle("menu-open", aberto);

                toggle.setAttribute("aria-expanded", aberto ? "true" : "false");

                toggle.setAttribute(
                    "aria-label",
                    aberto ? "Fechar menu" : "Abrir menu"
                );

                if (aberto && fechar) {
                    requestAnimationFrame(function () { fechar.focus(); });
                } else if (!aberto && focoNoMenu) {
                    toggle.focus();
                }
            }

            toggle.addEventListener("click", function () {
                definirMenu(!menuAberto());
            });

            if (fechar) {
                fechar.addEventListener("click", function () {
                    definirMenu(false);
                });
            }

            if (backdrop) {
                backdrop.addEventListener("click", function () {
                    definirMenu(false);
                });
            }

            document.addEventListener("keydown", function (evento) {
                if (evento.key === "Escape" && menuAberto()) {
                    definirMenu(false);
                }
            });

            sidebar.addEventListener("click", function (evento) {
                if (evento.target.closest("a")) {
                    definirMenu(false);
                }
            });

            window
                .matchMedia("(min-width: 768px)")
                .addEventListener("change", function (evento) {
                    if (evento.matches) {
                        definirMenu(false);
                    }
                });
        }
    );
})();