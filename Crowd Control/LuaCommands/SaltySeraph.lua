---@class Game
---@type Server
---@param sm.localPlayer player
function SaltySeraph.Fast( self, params )
    params.player.character:setMovementSpeedFraction( params.speed )
    -- Delay in ticks
    Timer.Delay( 400, SaltySeraph.EndFast, params )
end

---@class Game
---@type Server
---@field Callback
function SaltySeraph.EndFast( self, params )
    params.character:setMovementSpeedFraction( 1 )
end

---@class Game
---@type Server
---@field Generic
---@param sm.localPlayer player
function SaltySeraph.Kit( self, params )
    params[1] = SaltySeraph.SearchKitParam(params[1])
    if params[1] == "/memekit" then
        local chest = sm.shape.createPart( obj_container_chest, params.player.character.worldPosition + sm.vec3.new( 0, 0, 5 ), sm.quat.identity() )
        chest.color = sm.color.new( 0, 1, 1 )
        local container = chest.interactable:getContainer()

        sm.container.beginTransaction()
        sm.container.collect( container, obj_resource_glowpoop, 100 )
        sm.container.collect( container, obj_pneumatic_pipe_03, 10 )
        sm.container.collect( container, obj_resource_glowpoop, 100 )
        sm.container.endTransaction()
    else 
        sm.event.sendToWorld( params.player.character:getWorld(), "sv_e_onChatCommand", params )
    end
end

---@field Global
function SaltySeraph.SearchKitParam(kit)
    local instruct = nil
    if kit == "meme" then
        instruct = "/memekit"
    elseif kit == "seed" then
        instruct = "/seedkit"
    elseif kit == "pipe" then
        instruct = "/pipekit"
    elseif kit == "food" then
        instruct = "/foodkit"
    elseif kit == "starter" then
        instruct = "/starterkit"
    elseif kit == "mechanic" then
        instruct = "/mechanicstartkit"
    end
    return instruct
end

---@class Game
---@type Server
function SaltySeraph.Rain( self, params )
    local bodies = sm.body.getAllBodies()
    for _, body in ipairs( bodies ) do
        local usable = body:isUsable()
        if usable then 
            local shape = body:getShapes()[1]
            if shape:getShapeUuid() == obj_interactive_propanetank_small or  shape:getShapeUuid() == obj_interactive_propanetank_large then
                sm.physics.explode( shape:getWorldPosition() , 7, 2.0, 6.0, 25.0, "RedTapeBot - ExplosivesHit" )
            end
        end
    end
    for i = 0, 150 do
        local bomb = sm.shape.createPart( obj_interactive_propanetank_large, params.location + sm.vec3.new( sm.noise.randomRange(-80,80), sm.noise.randomRange(-80,80), sm.noise.randomRange(35,250) ), sm.quat.identity() )
    end
end

---@class Game
---@type Server
function SaltySeraph.Shield( self, params )
    g_godMode = true
    Timer.Delay( 400, SaltySeraph.RemoveShield, params )
end

---@class Game
---@type Server
---@field Callback
function SaltySeraph.RemoveShield( self, params )
    g_godMode = false
end

---@class Player
---@type Server
function SaltySeraph.Slap( self, params )
    local direction = sm.vec3.new(sm.noise.randomRange(-1,1),sm.noise.randomRange(-1,1),sm.noise.randomRange(0,1))
    local force = sm.noise.randomRange(1000,7000) 
    self.player.character:setTumbling( true )
    Timer.Delay( 8, SaltySeraph.ImpulseSlap, {slap = direction * force} )
end

---@class Player
---@type Server
---@field Callback
function SaltySeraph.ImpulseSlap( self, params )
    self.player.character:applyTumblingImpulse( params.slap )
end

---@class Game
---@type Server
function SaltySeraph.Trip( self, params )
    if params[2] ~= nil then
        player.character:setTumbling( params.state )
    else
        player.character:setTumbling( not player.character:isTumbling() )
    end
    if player.character:isTumbling() then
        self.network:sendToClients( "client_showMessage", "Player is tumbling" )
    else
        self.network:sendToClients( "client_showMessage", "Player is not tumbling" )
    end
end