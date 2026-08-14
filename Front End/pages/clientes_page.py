"""
Configuração da página de Clientes.

Observe que quase não existe lógica aqui.

CrudPage já sabe:
    criar tabela
    listar
    consultar
    mostrar dados
    tratar erros

Clientes apenas informa:
    título
    campo ID
    colunas
"""

from components.crud_page import CrudPage


def criar_clientes_page(
    page,
    api,
):
    return CrudPage(
        page=page,
        api=api,

        titulo="Clientes",

        id_campo="idCliente",

        colunas=[
            ("ID", "idCliente"),
            ("Nome", "nome"),
            ("E-mail", "email"),
            ("Telefone", "telefone"),
        ],
    )
