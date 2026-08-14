from components.crud_page import CrudPage

def criar_equipamentos_page(page, api):
    return CrudPage(
        page=page,
        api=api,
        titulo="Equipamentos",
        # O .NET converte "IdEquipamento" para camelCase ("idEquipamento") no JSON de resposta
        id_campo="idEquipamento", 
        colunas=[
            ("ID", "idEquipamento"),
            ("ID Cliente", "idCliente"),
            ("Tipo", "tipo"),
            ("Marca", "marca"),
            ("Modelo", "modelo"),
            ("Nº Série", "numeroSerie"),
        ],
    )