ent-BaseRune = rune
    .desc = { "" }
    .suffix = { "" }
ent-CollideRune = collision rune
    .desc = { ent-BaseRune.desc }
    .suffix = { "" }
ent-ActivateRune = activation rune
    .desc = { ent-CollideRune.desc }
    .suffix = { "" }
ent-CollideTimerRune = collision timed rune
    .desc = { ent-CollideRune.desc }
    .suffix = { "" }
ent-ExplosionRune = explosion rune
    .desc = { ent-CollideRune.desc }
    .suffix = { "" }
ent-StunRune = stun rune
    .desc = { ent-CollideRune.desc }
    .suffix = { "" }
ent-IgniteRune = ignite rune
    .desc = { ent-CollideRune.desc }
    .suffix = { "" }
ent-ExplosionTimedRune = explosion timed rune
    .desc = { ent-CollideTimerRune.desc }
    .suffix = { "" }
ent-ExplosionActivateRune = explosion activated rune
    .desc = { ent-ActivateRune.desc }
    .suffix = { "" }
ent-FlashRune = flash rune
    .desc = { ent-ActivateRune.desc }
    .suffix = { "" }
ent-FlashRuneTimer = flash timed rune
    .desc = { ent-CollideTimerRune.desc }
    .suffix = { "" }
