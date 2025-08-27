graph TB
    subgraph "Message Broker (RabbitMQ)"
        Producer1["Produtor 1<br/>Sistema A"]
        Producer2["Produtor 2<br/>Sistema B"]
        
        Exchange["Exchange<br/>(Roteamento Inteligente)"]
        
        Queue1["Fila 1<br/>(Durável)"]
        Queue2["Fila 2<br/>(Durável)"]
        Queue3["Fila 3<br/>(Durável)"]
        
        Consumer1["Consumidor 1<br/>Sistema C"]
        Consumer2["Consumidor 2<br/>Sistema D"]
        Consumer3["Consumidor 3<br/>Sistema E"]
        
        DeadLetter["Dead Letter<br/>Queue"]
    end
    
    Producer1 -->|"Mensagem + Routing Key"| Exchange
    Producer2 -->|"Mensagem + Routing Key"| Exchange
    
    Exchange -->|"Roteamento por padrão"| Queue1
    Exchange -->|"Roteamento por tópico"| Queue2
    Exchange -->|"Roteamento direto"| Queue3
    
    Queue1 -->|"Entrega confiável"| Consumer1
    Queue2 -->|"Controle de fluxo"| Consumer2
    Queue3 -->|"Acknowledgment"| Consumer3
    
    Queue1 -.->|"Mensagens rejeitadas"| DeadLetter
    Queue2 -.->|"TTL expirado"| DeadLetter
    Queue3 -.->|"Max retries"| DeadLetter
    
    style Exchange fill:#e1f5fe
    style DeadLetter fill:#ffebee
    style Queue1 fill:#f3e5f5
    style Queue2 fill:#f3e5f5
    style Queue3 fill:#f3e5f5
