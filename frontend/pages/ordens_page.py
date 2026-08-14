from components.crud_page import CrudPage

def criar_ordens_page(page, api):
    # Dica: Chamamos "Ordens" em vez de "Ordens de Serviço" 
    # para o texto automático gerar "Nova Ordem" e "Lista de Ordens" de forma perfeita.
    return CrudPage(
        page=page,
        api=api,
        titulo="Ordens",
        id_campo="idOrdem", 
        colunas=[
            ("ID", "idOrdem"),
            ("ID Equip.", "idEquipamento"),
            ("Defeito", "defeitoRelatado"),
            ("Status", "status"),
            ("Total (€)", "valorTotal"),
        ],
    )