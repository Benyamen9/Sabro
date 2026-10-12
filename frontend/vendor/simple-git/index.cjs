'use strict'
// See package.json: simple-git 4, callable as the v3 `require('simple-git')()`.
const v4 = require('simple-git-v4')

function simpleGit(...args) {
  return v4.simpleGit(...args)
}

module.exports = Object.assign(simpleGit, v4, { default: simpleGit })
