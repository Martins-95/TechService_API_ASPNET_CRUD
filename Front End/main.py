"""
TechService Frontend - V1 IEFP

Objetivo da V1:
    - manter a estrutura simples da V0;
    - mudar a interface para tema claro;
    - listar clientes com os botões Editar e Excluir;
    - permitir editar um cliente;
    - permitir excluir/desativar um cliente após confirmação.

Regra:
    main.py continua pequeno.

Executar:
    python main.py
"""

import flet as ft

from config import API_URL
from crud_api import CrudApi
from menu import AppMenu
from pages.clientes_page import criar_clientes_page
from pages.equipamentos_page import criar_equipamentos_page 
from pages.ordens_page import criar_ordens_page 


def main(page: ft.Page):
    # ==============================================================
    # V1 - TEMA CLARO
    # ==============================================================
    # Na V0 utilizávamos ThemeMode.DARK e fundo escuro.
    # Na V1 usamos uma tela branca, conforme o protótipo da aula.
    page.title = "TechService"
    page.theme_mode = ft.ThemeMode.LIGHT
    page.padding = 0
    page.spacing = 0
    page.bgcolor = "#FFFFFF"

    # Área que recebe a página escolhida no menu.
    conteudo = ft.Container(
        expand=True,
        bgcolor="#FFFFFF",
    )

    # ==============================================================
    # API DE CLIENTES
    # ==============================================================
    # CrudApi continua igual à V0.
    # A interface chama os métodos que já existiam:
    #   listar()
    #   buscar_por_id()
    #   atualizar()
    #   desativar()
    clientes_api = CrudApi(
        base_url=API_URL,
        endpoint="/api/clientes",
    )

    # ==============================================================
    # API DE EQUIPAMENTOS
    # ==============================================================
    equipamentos_api = CrudApi( 
        base_url=API_URL,
        endpoint="/api/equipamentos",
    )

    # ==============================================================
    # API DE ORDENS
    # ==============================================================
    ordens_api = CrudApi(
        base_url=API_URL,
        endpoint="/api/ordens-servico",
    )

    # ==============================================================
    # NAVEGAÇÃO
    # ==============================================================
    def abrir_clientes(e=None):
        pagina = criar_clientes_page(
            page=page,
            api=clientes_api,
        )
        conteudo.content = pagina.build()
        page.update()

        # Depois de montar a tela, busca os clientes na API.
        pagina.carregar()

    def abrir_equipamentos(e=None): # <-- NOVO
        pagina = criar_equipamentos_page(
            page=page, 
            api=equipamentos_api,
        )
        conteudo.content = pagina.build()
        page.update()

        # Depois de montar a tela, busca os clientes na API.
        pagina.carregar()

    def abrir_ordens(e=None): # <-- SUBSTITUA O pagina_futura() POR ISTO
        pagina = criar_ordens_page(page=page, api=ordens_api)
        conteudo.content = pagina.build()
        page.update()
        pagina.carregar()

    


    # ==============================================================
    # MENU
    # ==============================================================
    menu = AppMenu(
        abrir_clientes=abrir_clientes,
        abrir_equipamentos=abrir_equipamentos,
        abrir_ordens=abrir_ordens, 
    )

    # ==============================================================
    # LAYOUT PRINCIPAL
    # ==============================================================
    page.add(
        ft.Column(
            controls=[
                # V1 - cabeçalho claro.
                ft.Container(
                    bgcolor="#FFFFFF",
                    border=ft.Border(
                        bottom=ft.BorderSide(
                            width=1,
                            color="#E5E7EB",
                        )
                    ),
                    padding=ft.Padding.symmetric(
                        horizontal=20,
                        vertical=12,
                    ),
                    content=ft.Row(
                        controls=[
                            ft.Icon(
                                ft.Icons.HUB_OUTLINED,
                                color="#0B63CE",
                                size=24,
                            ),
                            ft.Text(
                                "Tech Service",
                                size=16,
                                weight=ft.FontWeight.W_600,
                                color="#111827",
                            ),
                        ],
                        spacing=10,
                    ),
                ),
                ft.Row(
                    controls=[
                        menu.build(),
                        conteudo,
                    ],
                    spacing=0,
                    expand=True,
                    vertical_alignment=ft.CrossAxisAlignment.START,
                ),
            ],
            spacing=0,
            expand=True,
        )
    )

    # Abre Clientes quando o programa inicia.
    abrir_clientes()


if __name__ == "__main__":
    ft.run(main)
