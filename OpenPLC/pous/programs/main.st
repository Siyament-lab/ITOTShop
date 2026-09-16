PROGRAM main
  VAR
    OrderId : UINT AT %QW0 := 0; (* Order-ID from integration program *)
    RequestedQuantity : UINT AT %QW1 := 0; (* Antal produkter som ska tillverkas *)
    ProductionStatus : UINT AT %QW2 := 0; (* 0=Idle, 1= Processing, 2=Completed, 3=Fault *)
    ProducedQuantity : UINT AT %QW3 := 0; (* Antal färdig producerade produkter *)
    FaultCode : UINT AT %QW4 := 0; (* Felkod från produktionsprocessen *)
    StartProduction : BOOL AT %QX0.0 := FALSE; (* Startsignal från integrationsprogrammet *)
    ResetProduction : BOOL AT %QX0.1 := FALSE; (* Återställer produktionsprocessen *)
    ProductionComplete : BOOL AT %QX0.2 := FALSE; (* Visar att ordern är färdig *)
    ProductionFault : BOOL AT %QX0.3 := FALSE; (* Visar att ett produktionsfel har uppstått *)
    ProductionTimer : TON; (* TTimer som simulerar produktionstiden *)
    ResetTimer : BOOL := FALSE; (* Återställer timern mellan varje produkt *)
  END_VAR

(* Återställer hela produktionsprocessen *)
IF ResetProduction then
ProductionStatus := 0;
ProducedQuantity := 0;
FaultCode := 0;
ProductionComplete := FALSE;
ProductionFault := FALSE;
ResetTimer := FALSE;

ProductionTimer(
    IN := False,
    PT := T#1s
);

else
        (* Starta endast när PLC:n är i Idle *)
    if StartProduction AND (ProductionStatus = 0) then
    (* Validera orderdata innan produktionen startar *)
    if OrderID = 0 then
    ProductionStatus := 3;
    FaultCode := 1;
    ProductionFault := TRUE;

elsif RequestedQuantity = 0 then
ProductionStatus := 3;
FaultCode := 2;
ProductionFault := TRUE;

elsif RequestedQuantity = 0 then
ProductionStatus :=3;
FaultCode := 2;
ProductionFault := TRUE;

ELSIF RequestedQuantity > 100 THEN
            ProductionStatus := 3;
            FaultCode := 3;
            ProductionFault := TRUE;

        ELSE
            ProductionStatus := 1;
            ProducedQuantity := 0;
            FaultCode := 0;
            ProductionComplete := FALSE;
            ProductionFault := FALSE;
            ResetTimer := FALSE;
end_if;
    
end_if;
(* Simulera att en produkt tillverkas varje sekund *)
    IF ProductionStatus = 1 THEN

        IF ResetTimer THEN
            ProductionTimer(
                IN := FALSE,
                PT := T#1s
            );

            ResetTimer := FALSE;

        ELSE
            ProductionTimer(
                IN := TRUE,
                PT := T#1s
            );

            IF ProductionTimer.Q THEN
                ProducedQuantity := ProducedQuantity + 1;
                ResetTimer := TRUE;

                (* Markera ordern som färdig *)
                IF ProducedQuantity >= RequestedQuantity THEN
                    ProductionStatus := 2;
                    ProductionComplete := TRUE;
                END_IF;
            END_IF;
        END_IF;

    ELSE
        ProductionTimer(
            IN := FALSE,
            PT := T#1s
        );

        ResetTimer := FALSE;
    END_IF;
END_IF;

END_PROGRAM